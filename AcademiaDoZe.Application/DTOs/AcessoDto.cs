using AcademiaDoZe.Application.Enums;
namespace AcademiaDoZe.Application.DTOs;
public sealed class PessoaAcessoDto
{
    public required int Id { get; init; }
    public required string Nome { get; init; }
    public required string Cpf { get; init; }
    public required AppPessoaTipo Tipo { get; init; }
    public ArquivoDto? Foto { get; init; }
    public override string ToString() => $"{Nome} - {Cpf}";
}
public sealed class AcessoDto
{
    public required PessoaAcessoDto Pessoa { get; init; }
    public required AppPessoaTipo Tipo { get; init; }
    public required string Movimento { get; init; }
    public required DateTime DataHora { get; init; }
}
public sealed record ResultadoAcessoDto(bool Autorizado, string Mensagem);
public sealed record EstadoAcessoDto(bool EstaDentro, DateTime? EntradaAbertaEm);
