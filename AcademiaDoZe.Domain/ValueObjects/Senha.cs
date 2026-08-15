// LEANDRO BATISTA CORDOVA DOS SANTOS
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Services;

namespace AcademiaDoZe.Domain.ValueObjects;

public record Senha
{
    public string Valor { get; }

    private Senha(string valor)
    {
        Valor = valor;
    }

    public static Result<Senha> Criar(string senha)
    {
        var notifications = new List<Notification>();

        if (NormalizacaoService.TextoVazioOuNulo(senha))
        {
            notifications.Add(new Notification("Senha", "SENHA_OBRIGATORIO"));
            return Result<Senha>.Failure(notifications);
        }

        if (senha.Length < 6) 
        {
            notifications.Add(new Notification("Senha", "TAMANHO_INVALIDO"));
        }

        if (!senha.Any(char.IsUpper))
        {
            notifications.Add(new Notification("Senha", "REQUER_LETRA_MAIUSCULA"));
        }

        if (notifications.Count > 0)
            return Result<Senha>.Failure(notifications);

        return Result<Senha>.Success(new Senha(senha));
    }
}