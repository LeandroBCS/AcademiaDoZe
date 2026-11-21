using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

public sealed class AcessoAlunoRepository(string connectionString, DatabaseType databaseType) : BaseRepository(connectionString, databaseType), IAcessoAlunoRepository
{
    private const int TipoAluno = 1;
    public async Task<AcessoAluno> Adicionar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        await using var cmd = await CreateCommandAsync(FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo,pessoa_id,data_hora) VALUES (@Tipo,@Pessoa,@Data)"), cancellationToken);
        cmd.AddParameter("@Tipo", TipoAluno, DbType.Int32); cmd.AddParameter("@Pessoa", entity.AlunoId, DbType.Int32); cmd.AddParameter("@Data", entity.DataHora, DbType.DateTime);
        var id = await cmd.ExecuteScalarIdAsync(cancellationToken: cancellationToken);
        return AcessoAluno.Reconstituir(id, entity.AlunoId, entity.DataHora);
    }
    public async Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        var sql = "SELECT id_acesso,pessoa_id,data_hora FROM tb_acesso WHERE pessoa_tipo=@Tipo";
        if (alunoId.HasValue) sql += " AND pessoa_id=@Pessoa";
        if (inicio.HasValue) sql += " AND data_hora >= @Inicio";
        if (fim.HasValue) sql += " AND data_hora < @Fim";
        sql += " ORDER BY data_hora,id_acesso";
        await using var cmd = await CreateCommandAsync(sql, cancellationToken); cmd.AddParameter("@Tipo", TipoAluno, DbType.Int32);
        if (alunoId.HasValue) cmd.AddParameter("@Pessoa", alunoId.Value, DbType.Int32);
        if (inicio.HasValue) cmd.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
        if (fim.HasValue) cmd.AddParameter("@Fim", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken); var result = new List<AcessoAluno>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(AcessoAluno.Reconstituir(Convert.ToInt32(reader["id_acesso"]), Convert.ToInt32(reader["pessoa_id"]), Convert.ToDateTime(reader["data_hora"])));
        return result;
    }
    public async Task<AcessoAluno?> ObterUltimoAcesso(int alunoId, CancellationToken cancellationToken = default) => (await ObterAcessosPorAlunoPeriodo(alunoId, cancellationToken: cancellationToken)).LastOrDefault();
    public async Task<bool> EstaNaAcademia(int alunoId, CancellationToken cancellationToken = default) => (await ObterAcessosPorAlunoPeriodo(alunoId, cancellationToken: cancellationToken)).Count() % 2 == 1;
    public Task<Dictionary<TimeOnly,int>> ObterHorarioMaisProcuradoPorMes(int mes, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<TimeOnly,int>());
    public Task<Dictionary<int,TimeSpan>> ObterPermanenciaMediaPorMes(int mes, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<int,TimeSpan>());
    public Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(int dias, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Aluno>>([]);
    public async Task<AcessoAluno?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        await using var cmd=await CreateCommandAsync("SELECT id_acesso,pessoa_id,data_hora FROM tb_acesso WHERE id_acesso=@Id AND pessoa_tipo=1",cancellationToken); cmd.AddParameter("@Id",id,DbType.Int32);
        await using var reader=await cmd.ExecuteReaderAsync(cancellationToken); return await reader.ReadAsync(cancellationToken)?AcessoAluno.Reconstituir(Convert.ToInt32(reader["id_acesso"]),Convert.ToInt32(reader["pessoa_id"]),Convert.ToDateTime(reader["data_hora"])):null;
    }
    public async Task<IEnumerable<AcessoAluno>> ObterTodos(CancellationToken cancellationToken = default) => await ObterAcessosPorAlunoPeriodo(cancellationToken: cancellationToken);
    public async Task<AcessoAluno> Atualizar(AcessoAluno entity, CancellationToken cancellationToken = default) { await using var cmd=await CreateCommandAsync("UPDATE tb_acesso SET pessoa_id=@Pessoa,data_hora=@Data WHERE id_acesso=@Id AND pessoa_tipo=1",cancellationToken); cmd.AddParameter("@Pessoa",entity.AlunoId,DbType.Int32); cmd.AddParameter("@Data",entity.DataHora,DbType.DateTime); cmd.AddParameter("@Id",entity.Id,DbType.Int32); await cmd.ExecuteNonQueryAsync(cancellationToken); return entity; }
    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default) { await using var cmd=await CreateCommandAsync("DELETE FROM tb_acesso WHERE id_acesso=@Id AND pessoa_tipo=1",cancellationToken); cmd.AddParameter("@Id",id,DbType.Int32); return await cmd.ExecuteNonQueryAsync(cancellationToken)>0; }
}
