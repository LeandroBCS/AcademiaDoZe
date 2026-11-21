using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

public partial class AcessoViewModel(IAcessoService service) : BaseViewModel
{
    public ObservableCollection<PessoaAcessoDto> Pessoas { get; } = [];
    public ObservableCollection<AcessoDto> Acessos { get; } = [];
    [ObservableProperty] private AppPessoaTipo tipoSelecionado = AppPessoaTipo.Aluno;
    [ObservableProperty] private PessoaAcessoDto? pessoaSelecionada;
    [ObservableProperty] private string busca = string.Empty;
    [ObservableProperty] private string mensagem = "Selecione uma pessoa para registrar o acesso.";
    [ObservableProperty] private bool acessoAutorizado;
    [ObservableProperty] private string textoAcao = "Registrar entrada";

    public async Task InicializarAsync()
    {
        await BuscarAsync();
        await AtualizarHistoricoAsync();
    }

    [RelayCommand]
    private async Task SelecionarAlunoAsync() { TipoSelecionado = AppPessoaTipo.Aluno; PessoaSelecionada = null; Busca = string.Empty; await BuscarAsync(); }
    [RelayCommand]
    private async Task SelecionarColaboradorAsync() { TipoSelecionado = AppPessoaTipo.Colaborador; PessoaSelecionada = null; Busca = string.Empty; await BuscarAsync(); }
    [RelayCommand]
    private async Task BuscarAsync()
    {
        Pessoas.Clear();
        foreach (var pessoa in await service.ObterPessoasAsync(TipoSelecionado, Busca)) Pessoas.Add(pessoa);
    }
    partial void OnBuscaChanged(string value) => BuscarAoDigitar();
    partial void OnPessoaSelecionadaChanged(PessoaAcessoDto? value) => AtualizarAcao();

    private async void BuscarAoDigitar() => await BuscarAsync();

    private async void AtualizarAcao()
    {
        if (PessoaSelecionada is null) { TextoAcao = "Registrar entrada"; return; }
        var hoje = await service.ObterHojeAsync();
        var quantos = hoje.Count(a => a.Tipo == PessoaSelecionada.Tipo && a.Pessoa.Id == PessoaSelecionada.Id);
        TextoAcao = quantos % 2 == 0 ? "Registrar entrada" : "Registrar saída";
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        if (PessoaSelecionada is null) { Mensagem = "Selecione uma pessoa."; AcessoAutorizado = false; return; }
        var resultado = await service.RegistrarAsync(TipoSelecionado, PessoaSelecionada.Id);
        Mensagem = resultado.Mensagem; AcessoAutorizado = resultado.Autorizado;
        if (resultado.Autorizado) await AtualizarHistoricoAsync();
        AtualizarAcao();
    }

    [RelayCommand] private Task AtualizarAsync() => AtualizarHistoricoAsync();
    private async Task AtualizarHistoricoAsync()
    {
        Acessos.Clear();
        foreach (var acesso in await service.ObterHojeAsync()) Acessos.Add(acesso);
    }
}