using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public sealed class AcessoService(Func<IAcessoAlunoRepository> alunoRepo, Func<IAcessoColaboradorRepository> colaboradorRepo,
    IAlunoService alunos, IColaboradorService colaboradores, IMatriculaService matriculas) : IAcessoService
{
    public async Task<IEnumerable<PessoaAcessoDto>> ObterPessoasAsync(AppPessoaTipo tipo,string busca,CancellationToken cancellationToken=default)
    {
        if(tipo==AppPessoaTipo.Aluno)
        {
            var lista=string.IsNullOrWhiteSpace(busca)?await alunos.ObterTodosAsync(cancellationToken):await alunos.ObterPorNomeAsync(busca,cancellationToken);
            return lista.Where(p=>string.IsNullOrWhiteSpace(busca)||p.Cpf.Contains(busca,StringComparison.OrdinalIgnoreCase)||p.Nome.Contains(busca,StringComparison.OrdinalIgnoreCase)).Select(p=>new PessoaAcessoDto{Id=p.Id,Nome=p.Nome,Cpf=p.Cpf,Tipo=tipo,Foto=p.Foto});
        }
        else
        {
            var lista=string.IsNullOrWhiteSpace(busca)?await colaboradores.ObterTodosAsync(cancellationToken):await colaboradores.ObterTodosAsync(cancellationToken);
            return lista.Where(p=>string.IsNullOrWhiteSpace(busca)||p.Cpf.Contains(busca,StringComparison.OrdinalIgnoreCase)||p.Nome.Contains(busca,StringComparison.OrdinalIgnoreCase)).Select(p=>new PessoaAcessoDto{Id=p.Id,Nome=p.Nome,Cpf=p.Cpf,Tipo=tipo,Foto=p.Foto});
        }
    }
    public async Task<IEnumerable<AcessoDto>> ObterHojeAsync(CancellationToken cancellationToken=default)
    {
        var hoje=DateOnly.FromDateTime(DateTime.Today); var saida=new List<AcessoDto>();
        var aa=await alunoRepo().ObterAcessosPorAlunoPeriodo(inicio:hoje,fim:hoje,cancellationToken:cancellationToken);
        var cc=await colaboradorRepo().ObterAcessosPorColaboradorPeriodo(inicio:hoje,fim:hoje,cancellationToken:cancellationToken);
        var movimentos=new Dictionary<(AppPessoaTipo Tipo,int Id),int>();
        foreach(var item in aa.Select(x=>(Tipo:AppPessoaTipo.Aluno,Id:x.AlunoId,Hora:x.DataHora)).Concat(cc.Select(x=>(Tipo:AppPessoaTipo.Colaborador,Id:x.ColaboradorId,Hora:x.DataHora))).OrderBy(x=>x.Hora))
        {
            var key=(item.Tipo,item.Id); movimentos.TryGetValue(key,out var ordem); var movimento=ordem%2==0?"Entrada":"Saída"; movimentos[key]=ordem+1;
            if(item.Tipo==AppPessoaTipo.Aluno){var p=await alunos.ObterPorIdAsync(item.Id,cancellationToken);if(p!=null)saida.Add(new(){Pessoa=new(){Id=p.Id,Nome=p.Nome,Cpf=p.Cpf,Tipo=item.Tipo,Foto=p.Foto},Tipo=item.Tipo,Movimento=movimento,DataHora=item.Hora});}
            else {var p=await colaboradores.ObterPorIdAsync(item.Id,cancellationToken);if(p!=null)saida.Add(new(){Pessoa=new(){Id=p.Id,Nome=p.Nome,Cpf=p.Cpf,Tipo=item.Tipo,Foto=p.Foto},Tipo=item.Tipo,Movimento=movimento,DataHora=item.Hora});}
        }
        return saida.OrderByDescending(x=>x.DataHora);
    }
    public async Task<ResultadoAcessoDto> RegistrarAsync(AppPessoaTipo tipo,int pessoaId,CancellationToken cancellationToken=default)
    {
        var agora=DateTime.Now;
        if(tipo==AppPessoaTipo.Aluno)
        {
            var aluno=await alunos.ObterPorIdAsync(pessoaId,cancellationToken);if(aluno is null)return new(false,"Aluno não encontrado.");
            var matricula=await matriculas.ObterMatriculaAtivaPorAlunoAsync(pessoaId,cancellationToken);if(matricula is null)return new(false,"Entrada negada: aluno sem matrícula ativa.");
            var eventos=(await alunoRepo().ObterAcessosPorAlunoPeriodo(pessoaId,cancellationToken:cancellationToken)).ToArray();
            if(eventos.Length%2==0){await alunoRepo().Adicionar(AcessoAluno.Reconstituir(0,pessoaId,agora),cancellationToken);var dias=Math.Max(0,matricula.DataFim.DayNumber-DateOnly.FromDateTime(agora).DayNumber);return new(true,$"Entrada registrada. Restam {dias} dia(s) de plano, até {matricula.DataFim:dd/MM/yyyy}.");}
            var duracao=agora-eventos[^1].DataHora;await alunoRepo().Adicionar(AcessoAluno.Reconstituir(0,pessoaId,agora),cancellationToken);return new(true,$"Saída registrada. Permanência: {duracao:hh\\:mm}.");
        }
        var colaborador=await colaboradores.ObterPorIdAsync(pessoaId,cancellationToken);if(colaborador is null)return new(false,"Colaborador não encontrado.");
        var repo=colaboradorRepo();var registros=(await repo.ObterAcessosPorColaboradorPeriodo(pessoaId,cancellationToken:cancellationToken)).ToArray();
        if(registros.Length%2==0)
        {
            var total=await repo.ObterHorasTrabalhadasNoDia(pessoaId,DateOnly.FromDateTime(agora),cancellationToken);var limite=colaborador.Vinculo==AppColaboradorVinculo.CLT?TimeSpan.FromHours(8):TimeSpan.FromHours(6);
            if(total>=limite)return new(false,$"Entrada negada: limite diário de {limite.TotalHours:0} horas já atingido.");
            await repo.Adicionar(AcessoColaborador.Reconstituir(0,pessoaId,agora),cancellationToken);return new(true,"Entrada registrada.");
        }
        var permanencia=agora-registros[^1].DataHora;await repo.Adicionar(AcessoColaborador.Reconstituir(0,pessoaId,agora),cancellationToken);var acumulado=await repo.ObterHorasTrabalhadasNoDia(pessoaId,DateOnly.FromDateTime(agora),cancellationToken);
        return new(true,$"Saída registrada. Permanência neste período: {permanencia:hh\\:mm}. Total no dia: {acumulado:hh\\:mm}.");
    }
}
