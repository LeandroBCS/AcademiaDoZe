using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
namespace AcademiaDoZe.Application.Interfaces;
public interface IAcessoService
{
    Task<IEnumerable<PessoaAcessoDto>> ObterPessoasAsync(AppPessoaTipo tipo, string busca, CancellationToken cancellationToken = default);
    Task<IEnumerable<AcessoDto>> ObterHojeAsync(CancellationToken cancellationToken = default);
    Task<ResultadoAcessoDto> RegistrarAsync(AppPessoaTipo tipo, int pessoaId, CancellationToken cancellationToken = default);
}
