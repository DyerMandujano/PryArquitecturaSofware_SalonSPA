using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pry_Solu_SalonSPA.Db;
using Pry_Solu_SalonSPA.Models;
using Pry_Solu_SalonSPA.ViewModels;
using System.Data;

namespace Pry_Solu_SalonSPA.Controllers
{
    public class VentasController : Controller
    {
        private readonly Conexion _context;

        public VentasController(Conexion context)
        {
            _context = context;
        }

        // GET: Index - Listar todas las ventas
        [HttpGet]
        public IActionResult Index(string tipoBusqueda = "TODOS", string valorBusqueda = "")
        {
            List<VentaListaVM> lista = new List<VentaListaVM>();
            var connectionString = _context.Database.GetConnectionString();

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();

                using (var cmd = new SqlCommand("SP_BuscarVentas", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TipoBusqueda", tipoBusqueda);
                    cmd.Parameters.AddWithValue("@ValorBusqueda", string.IsNullOrEmpty(valorBusqueda) ? (object)DBNull.Value : valorBusqueda);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var venta = new VentaListaVM
                            {
                                Id_Venta = reader.GetInt32(reader.GetOrdinal("Id_Venta")),
                                Fecha_Venta = reader.GetDateTime(reader.GetOrdinal("Fecha_Venta")),
                                Nombre_Cliente = reader.GetString(reader.GetOrdinal("Nombre_Cliente")),
                                Nombre_Empleado = reader.GetString(reader.GetOrdinal("Nombre_Empleado")),
                                Metodo_Pago = reader.GetString(reader.GetOrdinal("Metodo_Pago")),
                                Tipo_Comprobante = reader.GetString(reader.GetOrdinal("Tipo_Comprobante")),
                                Total_Productos = reader.GetInt32(reader.GetOrdinal("Total_Productos")),
                                Total_Servicios = reader.GetInt32(reader.GetOrdinal("Total_Servicios")),
                                Monto_Total = reader.GetDecimal(reader.GetOrdinal("Monto_Total"))
                            };

                            lista.Add(venta);
                        }
                    }
                }
            }

            return View(lista);
        }

        // GET: Registrar Venta
        [HttpGet]
        public IActionResult Registrar()
        {

            var vm = new VentaRegistrarVM
            {
                FechaVenta = DateTime.Now,
                Empleados = _context.Empleados
                    .Include(e => e.IdPersonaNavigation)
                    .Where(e => e.FechaRetiro == null) // Solo empleados activos
                    .ToList(),
                TiposPago = _context.TipoPagos
                    .Where(tp => tp.Estado == 1)
                    .ToList(),
                TiposComprobante = _context.TipoComprobantes
                    .Where(tc => tc.Estado == 1)
                    .ToList(),
                Productos = _context.Productos
                    .Include(p => p.IdCategoriaNavigation)
                    .Include(p => p.IdMarcaNavigation)
                    .Where(p => p.Estado == 1 && p.Stock > 0)
                    .ToList(),
                Clientes = _context.Clientes
                    .Include(c => c.IdPersonaNavigation)
                    .ToList(),
                Detalles = new List<VentaDetalleVM>()
            };

            return View(vm);
        }

        // POST: Registrar Venta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar(VentaRegistrarVM vm)
        {
            // Validación personalizada: Debe tener al menos un detalle
            if (vm.Detalles == null || !vm.Detalles.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto o servicio a la venta.");
            }

            // Validación: Debe tener cliente seleccionado
            if (!vm.IdCliente.HasValue || vm.IdCliente.Value == 0)
            {
                ModelState.AddModelError("IdCliente", "Debe seleccionar un cliente.");
            }

            if (!ModelState.IsValid)
            {
                // Recargar listas para la vista
                vm.Empleados = _context.Empleados
                    .Include(e => e.IdPersonaNavigation)
                    .Where(e => e.FechaRetiro == null)
                    .ToList();
                vm.TiposPago = _context.TipoPagos
                    .Where(tp => tp.Estado == 1)
                    .ToList();
                vm.TiposComprobante = _context.TipoComprobantes
                    .Where(tc => tc.Estado == 1)
                    .ToList();
                vm.Productos = _context.Productos
                    .Include(p => p.IdCategoriaNavigation)
                    .Include(p => p.IdMarcaNavigation)
                    .Where(p => p.Estado == 1 && p.Stock > 0)
                    .ToList();
                vm.Clientes = _context.Clientes
                    .Include(c => c.IdPersonaNavigation)
                    .ToList();

                return View(vm);
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Parámetro OUT para capturar el IdVenta
                    var idVentaParam = new SqlParameter
                    {
                        ParameterName = "@Id_Venta",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output
                    };

                    // Insertar cabecera de venta
                    _context.Database.ExecuteSqlRaw(
                        "EXEC SP_InsertarVenta @Id_empleado, @Id_cliente, @Id_Tipo_Pago, @Id_TipoCompro, @Fecha_registro, @Id_Venta OUT",
                        new SqlParameter("@Id_empleado", vm.IdEmpleado),
                        new SqlParameter("@Id_cliente", vm.IdCliente.Value),
                        new SqlParameter("@Id_Tipo_Pago", vm.IdTipoPago),
                        new SqlParameter("@Id_TipoCompro", vm.IdTipoComprobante),
                        new SqlParameter("@Fecha_registro", vm.FechaVenta),
                        idVentaParam
                    );

                    int idVenta = (int)idVentaParam.Value;

                    // Validar que se haya generado el ID
                    if (idVenta == 0)
                    {
                        throw new Exception("No se pudo generar el ID de la venta");
                    }

                    // Insertar detalles
                    foreach (var detalle in vm.Detalles)
                    {
                        // Validar stock para productos
                        if (detalle.TipoItem == "Producto" && detalle.IdProducto.HasValue)
                        {
                            var producto = _context.Productos.Find(detalle.IdProducto.Value);
                            if (producto == null)
                            {
                                throw new Exception($"El producto con ID {detalle.IdProducto} no existe");
                            }
                            if (producto.Stock < detalle.Cantidad)
                            {
                                throw new Exception($"Stock insuficiente para el producto {producto.NomProd}. Stock disponible: {producto.Stock}");
                            }
                        }

                        _context.Database.ExecuteSqlRaw(
                            "EXEC SP_InsertarDetalleVenta @Id_Venta, @Id_TipoCompro, @Id_Producto, @Id_CitaServicio, @Cantidad, @Precio_Venta",
                            new SqlParameter("@Id_Venta", idVenta),
                            new SqlParameter("@Id_TipoCompro", vm.IdTipoComprobante),
                            new SqlParameter("@Id_Producto", detalle.TipoItem == "Producto" && detalle.IdProducto.HasValue ? detalle.IdProducto.Value : DBNull.Value),
                            new SqlParameter("@Id_CitaServicio", detalle.TipoItem == "Servicio" && detalle.IdCitaServicio.HasValue ? detalle.IdCitaServicio.Value : DBNull.Value),
                            new SqlParameter("@Cantidad", detalle.Cantidad),
                            new SqlParameter("@Precio_Venta", detalle.PrecioUnitario)
                        );
                    }

                    // Si todo salió bien, confirmar transacción
                    transaction.Commit();

                    TempData["Mensaje"] = "Venta registrada correctamente";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Si hay error, revertir transacción
                    transaction.Rollback();
                    TempData["Error"] = $"Ocurrió un error: {ex.Message}";

                    // Recargar listas
                    vm.Empleados = _context.Empleados
                        .Include(e => e.IdPersonaNavigation)
                        .Where(e => e.FechaRetiro == null)
                        .ToList();
                    vm.TiposPago = _context.TipoPagos
                        .Where(tp => tp.Estado == 1)
                        .ToList();
                    vm.TiposComprobante = _context.TipoComprobantes
                        .Where(tc => tc.Estado == 1)
                        .ToList();
                    vm.Productos = _context.Productos
                        .Include(p => p.IdCategoriaNavigation)
                        .Include(p => p.IdMarcaNavigation)
                        .Where(p => p.Estado == 1 && p.Stock > 0)
                        .ToList();
                    vm.Clientes = _context.Clientes
                        .Include(c => c.IdPersonaNavigation)
                        .ToList();

                    return View(vm);
                }
            }
        }

        // GET: Buscar CitaServicio por ID (AJAX)
        [HttpGet]
        public IActionResult BuscarCitaServicio(int idCitaServicio)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                CitaServicioBusquedaVM resultado = null;

                using (var cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    using (var cmd = new SqlCommand("SP_BuscarCitaServicio", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id_CitaServicio", idCitaServicio);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                resultado = new CitaServicioBusquedaVM
                                {
                                    Id_CitaServicio = reader.GetInt32(reader.GetOrdinal("Id_CitaServicio")),
                                    Id_Cliente = reader.GetInt32(reader.GetOrdinal("Id_Cliente")),
                                    Nombre_Cliente = reader.GetString(reader.GetOrdinal("Nombre_Cliente")),
                                    Telefono = reader.GetString(reader.GetOrdinal("Telefono")),
                                    Dni = reader.GetString(reader.GetOrdinal("Dni")),
                                    Id_Servicio = reader.GetInt32(reader.GetOrdinal("Id_Servicio")),
                                    Nombre_Servicio = reader.GetString(reader.GetOrdinal("Nombre_Servicio")),
                                    Descripcion_Servicio = reader.GetString(reader.GetOrdinal("Descripcion_Servicio")),
                                    Precio_Servicio = reader.GetDecimal(reader.GetOrdinal("Precio_Servicio"))
                                };
                            }
                        }
                    }
                }

                if (resultado == null)
                {
                    return Json(new { success = false, message = "No se encontró la cita o ya fue utilizada" });
                }

                return Json(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: Obtener datos de un producto por ID (AJAX)
        [HttpGet]
        public IActionResult ObtenerProducto(int idProducto)
        {
            try
            {
                var producto = _context.Productos
                    .Include(p => p.IdCategoriaNavigation)
                    .Include(p => p.IdMarcaNavigation)
                    .Where(p => p.IdProducto == idProducto && p.Estado == 1)
                    .Select(p => new
                    {
                        p.IdProducto,
                        p.NomProd,
                        p.Precio,
                        p.Stock,
                        Categoria = p.IdCategoriaNavigation.NomCate,
                        Marca = p.IdMarcaNavigation.NomMarca
                    })
                    .FirstOrDefault();

                if (producto == null)
                {
                    return Json(new { success = false, message = "Producto no encontrado" });
                }

                return Json(new { success = true, data = producto });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}