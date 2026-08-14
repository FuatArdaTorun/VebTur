using System.Text.RegularExpressions;
using VebTur.Application.Reservations;

namespace VebTur.UnitTests.Reservations;

public partial class ReferenceNumberGeneratorTests
{
    [Fact]
    public void Generate_ProducesExpectedFormat()
    {
        var reference = ReferenceNumberGenerator.Generate();

        Assert.Matches(FormatRegex(), reference);
    }

    [Fact]
    public void Generate_DoesNotProduceAmbiguousCharacters()
    {
        for (var i = 0; i < 200; i++)
        {
            var reference = ReferenceNumberGenerator.Generate();
            Assert.DoesNotContain('0', reference);
            Assert.DoesNotContain('O', reference);
            Assert.DoesNotContain('1', reference);
            Assert.DoesNotContain('I', reference);
        }
    }

    [Fact]
    public void Generate_ProducesVaryingValues()
    {
        var values = Enumerable.Range(0, 50).Select(_ => ReferenceNumberGenerator.Generate()).ToHashSet();

        Assert.True(values.Count > 1, "Expected multiple distinct reference numbers across 50 generations.");
    }

    [GeneratedRegex("^VEB-[A-HJ-NP-Z2-9]{8}$")]
    private static partial Regex FormatRegex();
}
