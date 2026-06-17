using FogonDesk.Application.Catalogs;
using FogonDesk.Application.Utilities;
using Xunit;

namespace FogonDesk.Tests.Unit
{
    public sealed class PlaceholderTests
    {
        [Fact]
        public void FolioGeneratorDebeGenerarFormatoLegible()
        {
            var generator = new FolioGenerator();

            var folio = generator.Generate("vta", new System.DateTime(2026, 6, 8, 12, 30, 0, System.DateTimeKind.Utc), 27);

            Assert.Equal("VTA-20260608-000027", folio);
        }

        [Fact]
        public void PlantillaPizzeriaDebeIncluirCategoriasYProductosDemo()
        {
            var template = BusinessTemplateCatalog.GetTemplate("pizzeria");

            Assert.Equal("pizzeria", template.Code);
            Assert.Contains(template.SuggestedCategories, item => item.Name == "Pizzas");
            Assert.Contains(template.SuggestedProducts, item => item.Name == "Pizza pepperoni mediana");
        }
    }
}
