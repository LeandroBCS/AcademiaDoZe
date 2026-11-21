using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using System.Data;

namespace AcademiaDoZe.Infrastructure.Repositories;

public sealed class AcessoColaboradorRepository(string connectionString, DatabaseType databaseType) : BaseRepository(connectionString, databaseType), IAcessoColaboradorRepository
{
    public async Task<AcessoColaborador> Adicionar(AcessoColaborador entity, CancellationToken cancellationToken = default)
    {
        await using var cmd=await CreateCommandAsync(FormatInsertQuery("INSERT INTO tb_acesso (pessoa_tipo,pessoa_id,data_hora) VALUES (2,@Pessoa,@Data)"),cancellationToken);
        cmd.AddParameter("@Pessoa",entity.ColaboradorId,DbType.Int32); cmd.AddParameter("@Data",entity.DataHora,DbType.DateTime);
        var id=await cmd.ExecuteScalarIdAsync(cancellationToken:cancellationToken); return AcessoColaborador.Reconstituir(id,entity.ColaboradorId,entity.DataHora);
    }
    public async Task<IEnumerable<AcessoColaborador>> ObterAcessosPorColaboradorPeriodo(int? colaboradorId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        var sql="SELECT id_acesso,pessoa_id,data_hora FROM tb_acesso WHERE pessoa_tipo=2";
        if(colaboradorId.HasValue) sql+=" AND pessoa_id=@Pessoa"; if(inicio.HasValue) sql+=" AND data_hora>=@Inicio"; if(fim.HasValue) sql+=" AND data_hora<@Fim"; sql+=" ORDER BY data_hora,id_acesso";
        await using var cmd=await CreateCommandAsync(sql,cancellationToken);
        if(colaboradorId.HasValue) cmd.AddParameter("@Pessoa",colaboradorId.Value,DbType.Int32); if(inicio.HasValue) cmd.AddParameter("@Inicio",inicio.Value.ToDateTime(TimeOnly.MinValue),DbType.DateTime); if(fim.HasValue) cmd.AddParameter("@Fim",fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),DbType.DateTime);
        await using var reader=await cmd.ExecuteReaderAsync(cancellationToken); var list=new List<AcessoColaborador>(); while(await reader.ReadAsync(cancellationToken)) list.Add(AcessoColaborador.Reconstituir(Convert.ToInt32(reader["id_acesso"]),Convert.ToInt32(reader["pessoa_id"]),Convert.ToDateTime(reader["data_hora"]))); return list;
    }
    public async Task<AcessoColaborador?> ObterUltimoAcesso(int colaboradorId,CancellationToken cancellationToken=default)=>(await ObterAcessosPorColaboradorPeriodo(colaboradorId,cancellationToken:cancellationToken)).LastOrDefault();
    public async Task<TimeSpan> ObterHorasTrabalhadasNoDia(int colaboradorId,DateOnly data,CancellationToken cancellationToken=default)
    {
        var eventos=(await ObterAcessosPorColaboradorPeriodo(colaboradorId,cancellationToken:cancellationToken))
            .Select(x=>x.DataHora).Order().ToArray();
        var inicioDia=data.ToDateTime(TimeOnly.MinValue);
        var fimDia=inicioDia.AddDays(1);
        var agora=DateTime.Now;
        var total=TimeSpan.Zero;

        for(var i=0;i<eventos.Length;i+=2)
        {
            var inicio=eventos[i];
            var fim=i+1<eventos.Length?eventos[i+1]: (agora<fimDia?agora:fimDia);
            if(fim<=inicio) continue;
            var trechoInicio=inicio>inicioDia?inicio:inicioDia;
            var trechoFim=fim<fimDia?fim:fimDia;
            if(trechoFim>trechoInicio) total+=trechoFim-trechoInicio;
        }
        return total;
    }
    public async Task<AcessoColaborador?> ObterPorId(int id,CancellationToken cancellationToken=default){await using var cmd=await CreateCommandAsync("SELECT id_acesso,pessoa_id,data_hora FROM tb_acesso WHERE id_acesso=@Id AND pessoa_tipo=2",cancellationToken);cmd.AddParameter("@Id",id,DbType.Int32);await using var reader=await cmd.ExecuteReaderAsync(cancellationToken);return await reader.ReadAsync(cancellationToken)?AcessoColaborador.Reconstituir(Convert.ToInt32(reader["id_acesso"]),Convert.ToInt32(reader["pessoa_id"]),Convert.ToDateTime(reader["data_hora"])):null;}
    public async Task<IEnumerable<AcessoColaborador>> ObterTodos(CancellationToken cancellationToken=default)=>await ObterAcessosPorColaboradorPeriodo(cancellationToken:cancellationToken);
    public async Task<AcessoColaborador> Atualizar(AcessoColaborador entity,CancellationToken cancellationToken=default){await using var cmd=await CreateCommandAsync("UPDATE tb_acesso SET pessoa_id=@Pessoa,data_hora=@Data WHERE id_acesso=@Id AND pessoa_tipo=2",cancellationToken);cmd.AddParameter("@Pessoa",entity.ColaboradorId,DbType.Int32);cmd.AddParameter("@Data",entity.DataHora,DbType.DateTime);cmd.AddParameter("@Id",entity.Id,DbType.Int32);await cmd.ExecuteNonQueryAsync(cancellationToken);return entity;}
    public async Task<bool> Remover(int id,CancellationToken cancellationToken=default){await using var cmd=await CreateCommandAsync("DELETE FROM tb_acesso WHERE id_acesso=@Id AND pessoa_tipo=2",cancellationToken);cmd.AddParameter("@Id",id,DbType.Int32);return await cmd.ExecuteNonQueryAsync(cancellationToken)>0;}
}
