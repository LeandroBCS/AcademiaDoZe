// LEANDRO BATISTA CORDOVA DOS SANTOS
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Enums;

namespace AcademiaDoZe.Domain.Entities;

public class Matricula : Entity
{
    public Aluno Aluno { get; private set; }
    public PlanoPlano Plano { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public bool Ativa { get; private set; }

    private Matricula(int id, Aluno aluno, PlanoPlano plano, DateOnly dataInicio, DateOnly dataFim, bool ativa) : base(id)
    {
        Aluno = aluno;
        Plano = plano;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Ativa = ativa;
    }

    public static Result<Matricula> Criar(int id, Aluno aluno, PlanoPlano plano, DateOnly dataInicio, DateOnly dataFim, bool ativa)
    {
        var notifications = new List<Notification>();

        if (aluno == null)
            notifications.Add(new Notification("Aluno", "ALUNO_OBRIGATORIO"));

        if (!Enum.IsDefined(plano))
            notifications.Add(new Notification("Plano", "PLANO_INVALIDO"));

        if (dataInicio == default)
            notifications.Add(new Notification("DataInicio", "DATA_INICIO_OBRIGATORIA"));

        if (dataFim == default)
            notifications.Add(new Notification("DataFim", "DATA_FIM_OBRIGATORIA"));
        else if (dataFim <= dataInicio)
            notifications.Add(new Notification("DataFim", "DATA_FIM_MENOR_OU_IGUAL_INICIO"));

        if (notifications.Count != 0)
            return Result<Matricula>.Failure(notifications);

        return Result<Matricula>.Success(new Matricula(id, aluno!, plano, dataInicio, dataFim, ativa));
    }
}