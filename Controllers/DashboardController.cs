using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using Pry_Solu_SalonSPA.Db;

namespace Pry_Solu_SalonSPA.Controllers
{
    public class DashboardController : Controller
    {
        private readonly Conexion _context;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DashboardController(Conexion context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("ConexionDB");
        }

        public IActionResult Index(int? anio)
        {
            int anioSeleccionado = anio ?? DateTime.Now.Year;

            ViewBag.CitasHoy = ObtenerCitasHoy();
            ViewBag.VentasDia = ObtenerVentasDia();
            ViewBag.ClientesActivos = ObtenerClientesActivos();
            ViewBag.AlertasInventario = ObtenerAlertasInventario(10);

            ViewBag.VentasMensuales = ObtenerVentasMensuales(anioSeleccionado);
            ViewBag.ServiciosMasSolicitados = ObtenerServiciosMasSolicitados(5);
            ViewBag.ProductosBajoStock = ObtenerProductosPorStock(10);

            ViewBag.AnioActual = anioSeleccionado;
            ViewBag.AniosDisponibles = ObtenerAniosDisponibles();

            return View();
        }

        private int ObtenerCitasHoy()
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_Dashboard_CitasHoy", connection);
            command.CommandType = CommandType.StoredProcedure;

            connection.Open();
            var result = command.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        private decimal ObtenerVentasDia()
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_Dashboard_VentasDia", connection);
            command.CommandType = CommandType.StoredProcedure;

            connection.Open();
            var result = command.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }

        private int ObtenerClientesActivos()
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_Dashboard_ClientesActivos", connection);
            command.CommandType = CommandType.StoredProcedure;

            connection.Open();
            var result = command.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        private int ObtenerAlertasInventario(int stockMinimo)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_Dashboard_AlertasInventario", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@StockMinimo", stockMinimo);

            connection.Open();
            var result = command.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        private List<dynamic> ObtenerVentasMensuales(int anio)
        {
            var resultado = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_Dashboard_VentasMensuales", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Anio", anio);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                resultado.Add(new
                {
                    Mes = reader.GetInt32(0),
                    NombreMes = reader.GetString(1),
                    TotalVentas = reader.GetDecimal(2),
                    CantidadVentas = reader.GetInt32(3)
                });
            }
            return resultado;
        }

        private List<dynamic> ObtenerServiciosMasSolicitados(int top)
        {
            var resultado = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_Dashboard_ServiciosMasSolicitados", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Top", top);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                resultado.Add(new
                {
                    TipoServicio = reader.GetString(0),
                    Cantidad = reader.GetInt32(1),
                    Ingreso = reader.GetDecimal(2)
                });
            }
            return resultado;
        }

        private List<dynamic> ObtenerProductosPorStock(int top)
        {
            var resultado = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_Dashboard_ProductosPorStock", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Top", top);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                resultado.Add(new
                {
                    Producto = reader.GetString(0),
                    Stock = reader.GetInt32(1),
                    Precio = reader.GetDecimal(2),
                    Nivel = reader.GetString(3)
                });
            }
            return resultado;
        }

        private List<int> ObtenerAniosDisponibles()
        {
            var anios = new List<int>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_Dashboard_AniosDisponibles", connection);
            command.CommandType = CommandType.StoredProcedure;

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                anios.Add(reader.GetInt32(0));
            }

            if (anios.Count == 0)
            {
                anios.Add(DateTime.Now.Year);
            }

            return anios;
        }
    }
}
