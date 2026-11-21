using AcademiaDoZe.Presentation.AppMaui.Views;
using Microsoft.Extensions.DependencyInjection;
namespace AcademiaDoZe.Presentation.AppMaui;

public partial class AppShell : Shell
{
    private readonly IServiceProvider _services;

    public AppShell(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
        ConfigurePageFactories();
        RegisterRoutes();

    }
    private void ConfigurePageFactories()
    {
        foreach (var section in Items.OfType<FlyoutItem>().SelectMany(item => item.Items))
        foreach (var content in section.Items)
        {
            content.ContentTemplate = content.Route switch
            {
                "dashboard" => new DataTemplate(() => _services.GetRequiredService<DashboardListPage>()),
                "logradouros" => new DataTemplate(() => _services.GetRequiredService<LogradouroListPage>()),
                "colaboradores" => new DataTemplate(() => _services.GetRequiredService<ColaboradorListPage>()),
                "alunos" => new DataTemplate(() => _services.GetRequiredService<AlunoListPage>()),
                "matriculas" => new DataTemplate(() => _services.GetRequiredService<MatriculaListPage>()),
                "acessos" => new DataTemplate(() => _services.GetRequiredService<AcessoPage>()),
                "configuracoes" => new DataTemplate(() => _services.GetRequiredService<ConfigPage>()),
                _ => content.ContentTemplate
            };
        }
    }

    // O Routing.RegisterRoute é necessário para que o Shell do MAUI reconheça e permita a navegação
    // para páginas que não estão diretamente no TabBar ou Flyout,
    // como páginas de detalhe, edição ou cadastro.
    private static void RegisterRoutes()
    {
        Routing.RegisterRoute("aluno", typeof(AlunoPage));
        Routing.RegisterRoute("logradouro", typeof(LogradouroPage));
        Routing.RegisterRoute("colaborador", typeof(ColaboradorPage));
        Routing.RegisterRoute("matricula", typeof(MatriculaPage));
    }
}
