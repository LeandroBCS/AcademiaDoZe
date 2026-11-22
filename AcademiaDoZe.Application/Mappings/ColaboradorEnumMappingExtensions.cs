using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Domain.Enums;
namespace AcademiaDoZe.Application.Mappings;

public static class ColaboradorEnumMappingExtensions
{
    public static ColaboradorTipo ToDomain(this AppColaboradorTipo appTipo)
    {
        return (ColaboradorTipo)appTipo;
    }
    public static AppColaboradorTipo ToApplication(this ColaboradorTipo domainTipo)
    {
        return (AppColaboradorTipo)domainTipo;
    }
    public static ColaboradorVinculo ToDomain(this AppColaboradorVinculo appVinculo)
    {
        return appVinculo switch
        {
            AppColaboradorVinculo.CLT => ColaboradorVinculo.CLT,
            AppColaboradorVinculo.Estagio => ColaboradorVinculo.Estagio,
            _ => throw new ArgumentOutOfRangeException(nameof(appVinculo), appVinculo, "Vínculo de colaborador inválido.")
        };
    }
    public static AppColaboradorVinculo ToApplication(this ColaboradorVinculo domainVinculo)
    {
        return domainVinculo switch
        {
            ColaboradorVinculo.CLT => AppColaboradorVinculo.CLT,
            ColaboradorVinculo.Estagio => AppColaboradorVinculo.Estagio,
            _ => throw new ArgumentOutOfRangeException(nameof(domainVinculo), domainVinculo, "Vínculo de colaborador inválido.")
        };
    }
}
