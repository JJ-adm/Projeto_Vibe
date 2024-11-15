using KMLFilterAPI.Models;
using SharpKml.Dom;
using SharpKml.Engine;

namespace KMLFilterAPI.Services

{
    public class KmlService
    {
        private readonly KmlFile _kmlFile;

        public KmlService(string kmlPath)
        {
            //carrega o arquivo KML
            using (var stream = File.OpenRead(kmlPath))
            {
                _kmlFile = KmlFile.Load(stream);
            }
        }

        // Valida os filtros fornecidos
        public void ValidateFilters(Filters filters)
        {
            // Obtem valores únicos para validação
            var validClientes = GetAvailableFilters()["Clientes"];
            var validSituacoes = GetAvailableFilters()["Situacoes"];
            var validBairros = GetAvailableFilters()["Bairros"];

            // Validação de CLIENTE
            if (!string.IsNullOrEmpty(filters.Cliente) && !validClientes.Contains(filters.Cliente))
            {
                throw new ArgumentException($"Cliente '{filters.Cliente}' não é válido.");
            }

            // Validação de SITUAÇÃO
            if (!string.IsNullOrEmpty(filters.Situacao) && !validSituacoes.Contains(filters.Situacao))
            {
                throw new ArgumentException($"Situação '{filters.Situacao}' não é válida.");
            }

            // Validação de BAIRRO
            if (!string.IsNullOrEmpty(filters.Bairro) && !validBairros.Contains(filters.Bairro))
            {
                throw new ArgumentException($"Bairro '{filters.Bairro}' não é válido.");
            }

            // Validação de REFERENCIA
            if (!string.IsNullOrEmpty(filters.Referencia) && filters.Referencia.Length < 3)
            {
                throw new ArgumentException($"Referencia deve ter pelo menos 3 caracteres.");
            }

            // Validação de RUA/CRUZAMENTO
            if (!string.IsNullOrEmpty(filters.RuaCruzamento) && filters.RuaCruzamento.Length < 3)
            {
                throw new ArgumentException($"Rua/Cruzamento deve ter pelo menos 3 caracteres.");
            }
        }

        //Metodo para obter placemarks filtrados
        public IEnumerable<Filters> GetFilteredPlacemarks(string cliente, string situacao, string bairro, string referencia, string ruaCruzamento)
        {
            var placemarks = _kmlFile.Root.Flatten().OfType<Placemark>();

            // Aplique os filtros aqui
            return placemarks.Select(p => new Filters
            {
                Cliente = GetExtendedData(p, "CLIENTE"),
                Situacao = GetExtendedData(p, "SITUAÇÃO"),
                Bairro = GetExtendedData(p, "BAIRRO"),
                Referencia = GetExtendedData(p, "REFERENCIA"),
                RuaCruzamento = GetExtendedData(p, "RUA/CRUZAMENTO"),
            }).Where(p =>
                (string.IsNullOrEmpty(cliente) || p.Cliente == cliente) &&
                (string.IsNullOrEmpty(situacao) || p.Situacao == situacao) &&
                (string.IsNullOrEmpty(bairro) || p.Bairro == bairro) &&
                (string.IsNullOrEmpty(referencia) || p.Referencia.Contains(referencia)) &&
                (string.IsNullOrEmpty(ruaCruzamento) || p.RuaCruzamento.Contains(ruaCruzamento))
            );
        }

        // Método para obter valores únicos dos filtros
        public Dictionary<string, List<string>> GetAvailableFilters()
        {
            var placemarks = _kmlFile.Root.Flatten().OfType<Placemark>();

            var clientes = placemarks.Select(p => GetExtendedData(p, "CLIENTE")).Distinct().ToList();
            var situacoes = placemarks.Select(p => GetExtendedData(p, "SITUAÇÃO")).Distinct().ToList();
            var bairros = placemarks.Select(p => GetExtendedData(p, "BAIRRO")).Distinct().ToList();

            return new Dictionary<string, List<string>>
        {
            { "Clientes", clientes },
            { "Situacoes", situacoes },
            { "Bairros", bairros }
        };
        }

        // Método para exportar placemarks filtrados para um novo arquivo KML
        public string ExportFilteredPlacemarks(Filters filters)
        {
            var placemarks = GetFilteredPlacemarks(filters.Cliente, filters.Situacao, filters.Bairro, filters.Referencia, filters.RuaCruzamento);

            var document = new Document();
            foreach (var placemark in placemarks)
            {
                var pm = new Placemark
                {
                    Name = placemark.Cliente,
                    Description = new Description
                    {
                        Text = $"Situação: {placemark.Situacao}, Bairro: {placemark.Bairro}"
                    },
                    Geometry = new Point() // Adapte para obter coordenadas do placemark original
                };
                document.AddFeature(pm);
            }

            var kml = new Kml { Feature = document };
            var kmlPath = "filtered.kml";

            using (var stream = File.Create(kmlPath))
            {
                KmlFile.Create(kml, false).Save(stream);
            }

            return kmlPath;
        }

        // Método auxiliar para obter valores de ExtendedData
        private string GetExtendedData(Placemark placemark, string name)
        {
            var data = placemark.ExtendedData.Data.FirstOrDefault(d => d.Name == name);
            return data?.Value;
        }

    }
}
