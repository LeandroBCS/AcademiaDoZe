using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AcessoViewModel(IAcessoService service) : BaseViewModel
{
    private CancellationTokenSource? _buscaCts;

    public ObservableCollection<PessoaAcessoDto> Pessoas { get; } = [];
    public ObservableCollection<AcessoDto> Acessos { get; } = [];

    [ObservableProperty] private AppPessoaTipo tipoSelecionado = AppPessoaTipo.Aluno;
    [ObservableProperty] private PessoaAcessoDto? pessoaSelecionada;
    [ObservableProperty] private string busca = string.Empty;
    [ObservableProperty] private string mensagem = "Selecione uma pessoa para consultar o estado de acesso.";
    [ObservableProperty] private string situacaoPessoa = "Selecione uma pessoa para consultar o estado.";
    [ObservableProperty] private string resultadoBusca = string.Empty;
    [ObservableProperty] private bool buscaSemResultados;
    [ObservableProperty] private bool acessoAutorizado;
    [ObservableProperty] private bool temResultado;
    [ObservableProperty] private string textoAcao = "Registrar entrada";

    public string TextoModo => TipoSelecionado == AppPessoaTipo.Aluno
        ? "Modo atual selecionado: Aluno"
        : "Modo atual selecionado: Colaborador";
    public bool TipoAlunoSelecionado => TipoSelecionado == AppPessoaTipo.Aluno;
    public bool PodeRegistrar => PessoaSelecionada is not null && !IsBusy;

    partial void OnTipoSelecionadoChanged(AppPessoaTipo value)
    {
        OnPropertyChanged(nameof(TextoModo));
        OnPropertyChanged(nameof(TipoAlunoSelecionado));
    }

    partial void OnPessoaSelecionadaChanged(PessoaAcessoDto? value)
    {
        OnPropertyChanged(nameof(PodeRegistrar));
        if (value is null)
        {
            TextoAcao = "Registrar entrada";
            SituacaoPessoa = "Nenhuma pessoa selecionada.";
            Acessos.Clear();
            return;
        }
        _ = AtualizarEstadoSelecionadoAsync(value);
        _ = AtualizarHistoricoAsync();
    }

    partial void OnBuscaChanged(string value)
    {
        _buscaCts?.Cancel();
        _buscaCts?.Dispose();
        _buscaCts = new CancellationTokenSource();
        _ = BuscarComAtrasoAsync(_buscaCts.Token);
    }

    public async Task InicializarAsync()
    {
        await BuscarPessoasAsync(CancellationToken.None);
        await AtualizarHistoricoAsync();
        if (PessoaSelecionada is not null)
            await AtualizarEstadoSelecionadoAsync(PessoaSelecionada);
    }

    [RelayCommand]
    private Task SelecionarAlunoAsync() => SelecionarTipoAsync(AppPessoaTipo.Aluno);

    [RelayCommand]
    private Task SelecionarColaboradorAsync() => SelecionarTipoAsync(AppPessoaTipo.Colaborador);

    private async Task SelecionarTipoAsync(AppPessoaTipo tipo)
    {
        TipoSelecionado = tipo;
        PessoaSelecionada = null;
        Mensagem = "Selecione uma pessoa para registrar o acesso.";
        TemResultado = false;
        await BuscarPessoasAsync(CancellationToken.None);
    }

    [RelayCommand]
    private Task BuscarAsync() => BuscarPessoasAsync(CancellationToken.None);

    private async Task BuscarComAtrasoAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            await BuscarPessoasAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
    }

    private async Task BuscarPessoasAsync(CancellationToken cancellationToken)
    {
        try
        {
            var pessoaAnterior = PessoaSelecionada;
            var pessoas = await service.ObterPessoasAsync(TipoSelecionado, Busca, cancellationToken);
            Pessoas.Clear();
            foreach (var pessoa in pessoas)
                Pessoas.Add(pessoa);

            if (pessoaAnterior is not null)
                PessoaSelecionada = Pessoas.FirstOrDefault(p => p.Id == pessoaAnterior.Id && p.Tipo == TipoSelecionado);

            BuscaSemResultados = Pessoas.Count == 0;
            ResultadoBusca = BuscaSemResultados
                ? "Nenhuma pessoa encontrada. Confira o nome ou CPF e tente novamente."
                : string.Empty;

            if (PessoaSelecionada is null && Pessoas.Count == 0)
                SituacaoPessoa = "Selecione uma pessoa encontrada para consultar o estado.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Mensagem = $"Não foi possível localizar pessoas: {ex.Message}";
            AcessoAutorizado = false;
            TemResultado = true;
        }
    }

    private async Task AtualizarEstadoSelecionadoAsync(PessoaAcessoDto pessoa)
    {
        try
        {
            var estado = await service.ObterEstadoAsync(pessoa.Tipo, pessoa.Id);
            TextoAcao = estado.EstaDentro ? "Registrar saída" : "Registrar entrada";
            SituacaoPessoa = estado.EstaDentro
                ? $"Dentro da academia desde {estado.EntradaAbertaEm:dd/MM/yyyy HH:mm}."
                : "A pessoa selecionada está fora da academia.";
        }
        catch (Exception ex)
        {
            Mensagem = $"Não foi possível consultar o estado de acesso: {ex.Message}";
            AcessoAutorizado = false;
            TemResultado = true;
        }
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        if (PessoaSelecionada is null)
        {
            Mensagem = "Selecione uma pessoa antes de registrar a entrada ou saída.";
            AcessoAutorizado = false;
            TemResultado = true;
            return;
        }

        try
        {
            IsBusy = true;
            OnPropertyChanged(nameof(PodeRegistrar));
            var resultado = await service.RegistrarAsync(TipoSelecionado, PessoaSelecionada.Id);
            Mensagem = resultado.Mensagem;
            AcessoAutorizado = resultado.Autorizado;
            TemResultado = true;
            await AtualizarEstadoSelecionadoAsync(PessoaSelecionada);
            if (resultado.Autorizado)
                await AtualizarHistoricoAsync();
        }
        catch (Exception ex)
        {
            Mensagem = $"Falha ao registrar o acesso: {ex.Message}";
            AcessoAutorizado = false;
            TemResultado = true;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(PodeRegistrar));
        }
    }

    [RelayCommand]
    private Task AtualizarAsync() => AtualizarHistoricoAsync();

    private async Task AtualizarHistoricoAsync()
    {
        var pessoa = PessoaSelecionada;
        Acessos.Clear();
        if (pessoa is null)
            return;

        try
        {
            var acessos = await service.ObterHojeAsync();
            // Não exiba acessos de terceiros. Também confira a seleção novamente,
            // pois ela pode ter mudado enquanto o histórico era carregado.
            if (PessoaSelecionada is not { } pessoaAtual
                || pessoaAtual.Id != pessoa.Id
                || pessoaAtual.Tipo != pessoa.Tipo)
                return;

            foreach (var acesso in acessos.Where(a => a.Pessoa.Id == pessoa.Id && a.Tipo == pessoa.Tipo))
                Acessos.Add(acesso);
        }
        catch (Exception ex)
        {
            Mensagem = $"Não foi possível carregar o histórico: {ex.Message}";
            AcessoAutorizado = false;
            TemResultado = true;
        }
    }
}
