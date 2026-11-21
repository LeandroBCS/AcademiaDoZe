using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public sealed class AcessoService(
    Func<IAcessoAlunoRepository> alunoRepo,
    Func<IAcessoColaboradorRepository> colaboradorRepo,
    IAlunoService alunos,
    IColaboradorService colaboradores,
    IMatriculaService matriculas) : IAcessoService
{
    public async Task<IEnumerable<PessoaAcessoDto>> ObterPessoasAsync(
        AppPessoaTipo tipo, string busca, CancellationToken cancellationToken = default)
    {
        var termo = busca.Trim();
        var cpfBusca = SomenteDigitos(termo);
        if (tipo == AppPessoaTipo.Aluno)
        {
            // Buscar no conjunto completo permite localizar também por CPF e evita
            // depender da implementação de busca por nome do repositório.
            var lista = await alunos.ObterTodosAsync(cancellationToken);

            return lista
                .Where(p => string.IsNullOrWhiteSpace(termo)
                    || p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase)
                    || (cpfBusca.Length > 0 && SomenteDigitos(p.Cpf).Contains(cpfBusca, StringComparison.Ordinal)))
                .OrderBy(p => p.Nome)
                .Select(p => new PessoaAcessoDto
                {
                    Id = p.Id, Nome = p.Nome, Cpf = p.Cpf, Tipo = tipo, Foto = p.Foto
                })
                .ToArray();
        }

        var colaboradoresLista = await colaboradores.ObterTodosAsync(cancellationToken);
        return colaboradoresLista
            .Where(p => string.IsNullOrWhiteSpace(termo)
                || p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || (cpfBusca.Length > 0 && SomenteDigitos(p.Cpf).Contains(cpfBusca, StringComparison.Ordinal)))
            .OrderBy(p => p.Nome)
            .Select(p => new PessoaAcessoDto
            {
                Id = p.Id, Nome = p.Nome, Cpf = p.Cpf, Tipo = tipo, Foto = p.Foto
            })
            .ToArray();
    }

    public async Task<EstadoAcessoDto> ObterEstadoAsync(
        AppPessoaTipo tipo, int pessoaId, CancellationToken cancellationToken = default)
    {
        if (tipo == AppPessoaTipo.Aluno)
        {
            await using var repo = alunoRepo();
            var eventos = (await repo.ObterAcessosPorAlunoPeriodo(pessoaId, cancellationToken: cancellationToken)).ToArray();
            return CriarEstado(eventos.Select(x => x.DataHora));
        }

        await using var colaboradorRepository = colaboradorRepo();
        var registros = (await colaboradorRepository.ObterAcessosPorColaboradorPeriodo(
            pessoaId, cancellationToken: cancellationToken)).ToArray();
        return CriarEstado(registros.Select(x => x.DataHora));
    }

    public async Task<IEnumerable<AcessoDto>> ObterHojeAsync(CancellationToken cancellationToken = default)
    {
        await using var alunoRepository = alunoRepo();
        await using var colaboradorRepository = colaboradorRepo();
        var eventosAluno = await alunoRepository.ObterAcessosPorAlunoPeriodo(cancellationToken: cancellationToken);
        var eventosColaborador = await colaboradorRepository.ObterAcessosPorColaboradorPeriodo(cancellationToken: cancellationToken);

        var todos = eventosAluno
            .Select(x => (Tipo: AppPessoaTipo.Aluno, Id: x.AlunoId, DataHora: x.DataHora))
            .Concat(eventosColaborador.Select(x => (Tipo: AppPessoaTipo.Colaborador, Id: x.ColaboradorId, DataHora: x.DataHora)))
            .OrderBy(x => x.DataHora)
            .ToArray();

        var agora = DateTime.Now;
        var hoje = DateOnly.FromDateTime(agora);
        var ordemPorPessoa = new Dictionary<(AppPessoaTipo Tipo, int Id), int>();
        var pessoas = new Dictionary<(AppPessoaTipo Tipo, int Id), PessoaAcessoDto>();
        var resultado = new List<AcessoDto>();

        foreach (var evento in todos)
        {
            var chave = (evento.Tipo, evento.Id);
            ordemPorPessoa.TryGetValue(chave, out var ordem);
            ordemPorPessoa[chave] = ordem + 1;

            if (DateOnly.FromDateTime(evento.DataHora) != hoje)
                continue;

            if (!pessoas.TryGetValue(chave, out var pessoa))
            {
                if (evento.Tipo == AppPessoaTipo.Aluno)
                {
                    var aluno = await alunos.ObterPorIdAsync(evento.Id, cancellationToken);
                    if (aluno is null) continue;
                    pessoa = new PessoaAcessoDto
                    {
                        Id = aluno.Id, Nome = aluno.Nome, Cpf = aluno.Cpf,
                        Tipo = evento.Tipo, Foto = aluno.Foto
                    };
                }
                else
                {
                    var colaborador = await colaboradores.ObterPorIdAsync(evento.Id, cancellationToken);
                    if (colaborador is null) continue;
                    pessoa = new PessoaAcessoDto
                    {
                        Id = colaborador.Id, Nome = colaborador.Nome, Cpf = colaborador.Cpf,
                        Tipo = evento.Tipo, Foto = colaborador.Foto
                    };
                }
                pessoas[chave] = pessoa;
            }

            resultado.Add(new AcessoDto
            {
                Pessoa = pessoa,
                Tipo = evento.Tipo,
                Movimento = ordem % 2 == 0 ? "Entrada" : "Saída",
                DataHora = evento.DataHora
            });
        }

        return resultado.OrderByDescending(x => x.DataHora).ToArray();
    }

    public async Task<ResultadoAcessoDto> RegistrarAsync(
        AppPessoaTipo tipo, int pessoaId, CancellationToken cancellationToken = default)
    {
        var agora = DateTime.Now;
        var hoje = DateOnly.FromDateTime(agora);

        if (tipo == AppPessoaTipo.Aluno)
        {
            if (await alunos.ObterPorIdAsync(pessoaId, cancellationToken) is null)
                return new(false, "Aluno não encontrado.");

            await using var repo = alunoRepo();
            var eventos = (await repo.ObterAcessosPorAlunoPeriodo(pessoaId, cancellationToken: cancellationToken)).ToArray();
            var estado = CriarEstado(eventos.Select(x => x.DataHora));

            // A saída precisa continuar disponível mesmo se o plano vencer durante a permanência.
            if (estado.EstaDentro)
            {
                var entrada = estado.EntradaAbertaEm!.Value;
                await repo.Adicionar(AcessoAluno.Reconstituir(0, pessoaId, agora), cancellationToken);
                return new(true, $"Saída registrada. Permanência: {FormatarDuracao(agora - entrada)}.");
            }

            var matricula = await matriculas.ObterMatriculaAtivaPorAlunoAsync(pessoaId, cancellationToken);
            if (matricula is null)
                return new(false, "Acesso negado: o aluno não possui matrícula ativa.");

            await repo.Adicionar(AcessoAluno.Reconstituir(0, pessoaId, agora), cancellationToken);
            var diasRestantes = Math.Max(0, matricula.DataFim.DayNumber - hoje.DayNumber);
            var prazo = diasRestantes == 0 ? "o plano vence hoje" : $"restam {diasRestantes} dia(s) de plano";
            return new(true, $"Entrada autorizada. {prazo} (validade: {matricula.DataFim:dd/MM/yyyy}).");
        }

        var colaborador = await colaboradores.ObterPorIdAsync(pessoaId, cancellationToken);
        if (colaborador is null)
            return new(false, "Colaborador não encontrado.");

        await using var colaboradorRepoAtual = colaboradorRepo();
        var registros = (await colaboradorRepoAtual.ObterAcessosPorColaboradorPeriodo(
            pessoaId, cancellationToken: cancellationToken)).ToArray();
        var estadoColaborador = CriarEstado(registros.Select(x => x.DataHora));
        var limite = colaborador.Vinculo == AppColaboradorVinculo.CLT
            ? TimeSpan.FromHours(8)
            : TimeSpan.FromHours(6);

        if (estadoColaborador.EstaDentro)
        {
            var entrada = estadoColaborador.EntradaAbertaEm!.Value;
            await colaboradorRepoAtual.Adicionar(AcessoColaborador.Reconstituir(0, pessoaId, agora), cancellationToken);
            var permanencia = agora - entrada;
            var totalDia = await colaboradorRepoAtual.ObterHorasTrabalhadasNoDia(pessoaId, hoje, cancellationToken);
            var avisoLimite = totalDia > limite
                ? $" Atenção: o limite diário de {FormatarDuracao(limite)} foi ultrapassado."
                : string.Empty;
            return new(true,
                $"Saída registrada. Permanência: {FormatarDuracao(permanencia)}. Total trabalhado hoje: {FormatarDuracao(totalDia)}.{avisoLimite}");
        }

        var trabalhadoHoje = await colaboradorRepoAtual.ObterHorasTrabalhadasNoDia(pessoaId, hoje, cancellationToken);
        if (trabalhadoHoje >= limite)
            return new(false,
                $"Entrada negada: o limite diário de {FormatarDuracao(limite)} já foi atingido (total atual: {FormatarDuracao(trabalhadoHoje)}).");

        await colaboradorRepoAtual.Adicionar(AcessoColaborador.Reconstituir(0, pessoaId, agora), cancellationToken);
        return new(true,
            $"Entrada autorizada. Total trabalhado hoje: {FormatarDuracao(trabalhadoHoje)} de {FormatarDuracao(limite)}.");
    }

    private static EstadoAcessoDto CriarEstado(IEnumerable<DateTime> eventos)
    {
        var ordenados = eventos.Order().ToArray();
        return ordenados.Length % 2 == 1
            ? new(true, ordenados[^1])
            : new(false, null);
    }

    private static string SomenteDigitos(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    private static string FormatarDuracao(TimeSpan duracao)
    {
        var minutos = Math.Max(0, (long)duracao.TotalMinutes);
        return $"{minutos / 60:00}:{minutos % 60:00}";
    }
}
