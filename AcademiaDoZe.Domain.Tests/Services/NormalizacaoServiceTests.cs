// LEANDRO BATISTA CORDOVA DOS SANTOS
using AcademiaDoZe.Domain.Services;

namespace AcademiaDoZe.Domain.Tests.Services;

public class NormalizacaoServiceTests
{
    [Theory(DisplayName = "NormalizacaoService: TextoVazioOuNulo -> valida nulo/vazio")]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("   ", true)]
    [InlineData("texto", false)]
    [InlineData("a", false)]
    public void Deve_TextoVazioOuNulo_RetornarEsperado(string? input, bool expected)
    {
        var result = NormalizacaoService.TextoVazioOuNulo(input);
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "NormalizacaoService: LimparEspacos -> normaliza espaços")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" a b c ", "a b c")]
    [InlineData("a\tb\nc", "a b c")]
    [InlineData("  Academia   do   Zé  ", "Academia do Zé")]
    public void Deve_Normalizar_Espacos_Quando_LimparEspacosChamado(string? input, string expected)
    {
        var result = NormalizacaoService.LimparEspacos(input);
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "NormalizacaoService: LimparTodosEspacos -> remove todos os espaços")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("a b c", "abc")]
    [InlineData(" a b ", "ab")]
    [InlineData(" S P ", "SP")]
    public void Deve_Remover_Todos_Espacos_Quando_LimparTodosEspacosChamado(string? input, string expected)
    {
        var result = NormalizacaoService.LimparTodosEspacos(input);
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "NormalizacaoService: ParaMaiusculo -> converte para maiúsculo")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("abc", "ABC")]
    [InlineData("áéíõç", "ÁÉÍÕÇ")]
    [InlineData("sc", "SC")]
    public void Deve_Converter_Para_Maiusculo_Quando_ParaMaiusculoChamado(string? input, string expected)
    {
        var result = NormalizacaoService.ParaMaiusculo(input);
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "NormalizacaoService: LimparEDigitos -> mantém apenas dígitos")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("a1b2c3", "123")]
    [InlineData("(11) 91234-5678", "11912345678")]
    [InlineData("no-digits", "")]
    [InlineData("88.500-000", "88500000")]
    public void Deve_Manter_Apenas_Digitos_Quando_LimparEDigitosChamado(string? input, string expected)
    {
        var result = NormalizacaoService.LimparEDigitos(input);
        Assert.Equal(expected, result);
    }
}