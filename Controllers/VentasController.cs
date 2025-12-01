using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pry_Solu_SalonSPA.Db;
using Pry_Solu_SalonSPA.Models;
using Pry_Solu_SalonSPA.ViewModels;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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
                    .Include(p => p.IdMarcaNavigation)
                    .Where(p => p.Estado == 1 && p.Stock > 0)
                    .ToList(),
                Clientes = _context.Clientes
                    .Include(c => c.IdPersonaNavigation)
                    .ToList(),
                Detalles = new List<VentaDetalleVM>()
            };

            return View("_RegistrarVentas",vm);
        }

        // POST: Registrar Venta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar(VentaRegistrarVM vm)
        {
            Console.WriteLine("🎯 =========== INICIANDO REGISTRO VENTA ===========");

            // DEBUG 1: Verificar datos recibidos
            Console.WriteLine($"📥 DATOS RECIBIDOS:");
            Console.WriteLine($"   IdEmpleado: {vm.IdEmpleado}");
            Console.WriteLine($"   IdCliente: {vm.IdCliente}");
            Console.WriteLine($"   IdTipoPago: {vm.IdTipoPago}");
            Console.WriteLine($"   IdTipoComprobante: {vm.IdTipoComprobante}");
            Console.WriteLine($"   FechaVenta: {vm.FechaVenta}");
            Console.WriteLine($"   Detalles count: {vm.Detalles?.Count ?? 0}");

            if (vm.Detalles != null)
            {
                foreach (var d in vm.Detalles)
                {
                    Console.WriteLine($"   📦 Detalle -> Tipo: {d.TipoItem}, Producto: {d.IdProducto}, Servicio: {d.IdCitaServicio}, Cant: {d.Cantidad}, Precio: {d.PrecioUnitario}");
                }
            }

            // Validaciones
            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ MODELSTATE INVALIDO");
                foreach (var error in ModelState)
                {
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"   🚫 {error.Key}: {err.ErrorMessage}");
                    }
                }
                RecargarDatos(vm);
                return View("_RegistrarVentas", vm);
            }

            if (vm.Detalles == null || !vm.Detalles.Any())
            {
                Console.WriteLine("❌ NO HAY DETALLES");
                RecargarDatos(vm);
                return View("_RegistrarVentas", vm);
            }

            // DEBUG 2: Verificar conexión
            var connectionString = _context.Database.GetConnectionString();
            Console.WriteLine($"🔗 Connection String: {connectionString}");

            using (var cn = new SqlConnection(connectionString))
            {
                try
                {
                    cn.Open();
                    Console.WriteLine("✅ CONEXIÓN A BD ABIERTA");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ERROR CONEXIÓN BD: {ex.Message}");
                    TempData["Error"] = "Error de conexión a la base de datos";
                    RecargarDatos(vm);
                    return View("_RegistrarVentas", vm);
                }

                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        int idVenta = 0;

                        // DEBUG 3: Ejecutar SP_InsertarVenta
                        Console.WriteLine("🔄 EJECUTANDO SP_InsertarVenta...");

                        using (var cmd = new SqlCommand("SP_InsertarVenta", cn, tx))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            // Parámetros
                            cmd.Parameters.AddWithValue("@Id_empleado", vm.IdEmpleado);
                            cmd.Parameters.AddWithValue("@Id_cliente", vm.IdCliente ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Id_Tipo_Pago", vm.IdTipoPago);
                            cmd.Parameters.AddWithValue("@Id_TipoCompro", vm.IdTipoComprobante);
                            cmd.Parameters.AddWithValue("@Fecha_registro", vm.FechaVenta);

                            // Parámetro OUTPUT
                            var outId = new SqlParameter("@Id_Venta", SqlDbType.Int)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(outId);

                            // Ejecutar
                            int filasAfectadas = cmd.ExecuteNonQuery();
                            Console.WriteLine($"   📊 Filas afectadas: {filasAfectadas}");

                            // Obtener resultado
                            if (outId.Value != DBNull.Value)
                            {
                                idVenta = (int)outId.Value;
                                Console.WriteLine($"   🆔 ID VENTA GENERADO: {idVenta}");
                            }
                            else
                            {
                                Console.WriteLine("   ❌ SP NO DEVOLVIÓ ID (DBNull.Value)");
                            }
                        }

                        // Verificar si se obtuvo ID
                        if (idVenta <= 0)
                        {
                            Console.WriteLine("❌ NO SE PUDO OBTENER ID VENTA VÁLIDO");
                            throw new Exception("No se pudo obtener el ID de la venta generado por el SP");
                        }

                        // DEBUG 4: Insertar detalles
                        Console.WriteLine($"🔄 INSERTANDO {vm.Detalles.Count} DETALLES...");
                        int contadorDetalles = 0;

                        foreach (var detalle in vm.Detalles)
                        {
                            contadorDetalles++;
                            Console.WriteLine($"   📝 Detalle {contadorDetalles}: Tipo={detalle.TipoItem}, Producto={detalle.IdProducto}, Servicio={detalle.IdCitaServicio}");

                            using (var cmd = new SqlCommand("SP_InsertarDetalleVenta", cn, tx))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Id_Venta", idVenta);
                                cmd.Parameters.AddWithValue("@Id_TipoCompro", vm.IdTipoComprobante);
                                cmd.Parameters.AddWithValue("@Id_Producto", detalle.IdProducto ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Id_CitaServicio", detalle.IdCitaServicio ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                cmd.Parameters.AddWithValue("@Precio_Venta", detalle.PrecioUnitario);

                                int filasDetalle = cmd.ExecuteNonQuery();
                                Console.WriteLine($"      ✅ Detalle {contadorDetalles} insertado. Filas: {filasDetalle}");
                            }
                        }

                        // DEBUG 5: Commit
                        tx.Commit();
                        Console.WriteLine("✅ TRANSACCIÓN COMPLETADA - COMMIT REALIZADO");
                        Console.WriteLine($"🎉 VENTA {idVenta} REGISTRADA EXITOSAMENTE CON {contadorDetalles} DETALLES");

                        TempData["Mensaje"] = "Venta registrada correctamente";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        // DEBUG 6: Error
                        Console.WriteLine($"❌ ERROR DURANTE TRANSACCIÓN: {ex}");
                        Console.WriteLine($"   Tipo: {ex.GetType().Name}");
                        Console.WriteLine($"   Mensaje: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                        }

                        tx.Rollback();
                        Console.WriteLine("🔄 TRANSACCIÓN CANCELADA - ROLLBACK REALIZADO");

                        TempData["Error"] = $"Error al registrar venta: {ex.Message}";
                        RecargarDatos(vm);
                        return View("_RegistrarVentas", vm);
                    }
                }
            }
        }

        private void RecargarDatos(VentaRegistrarVM vm)
        {
            vm.Empleados = _context.Empleados
                .Include(e => e.IdPersonaNavigation)
                .Where(e => e.FechaRetiro == null)
                .ToList();
            vm.TiposPago = _context.TipoPagos.Where(tp => tp.Estado == 1).ToList();
            vm.TiposComprobante = _context.TipoComprobantes.Where(tc => tc.Estado == 1).ToList();
            vm.Productos = _context.Productos
                .Include(p => p.IdMarcaNavigation)
                .Where(p => p.Estado == 1 && p.Stock > 0)
                .ToList();
            vm.Clientes = _context.Clientes.Include(c => c.IdPersonaNavigation).ToList();
            vm.Detalles ??= new List<VentaDetalleVM>();
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


        // GET: Ver detalle de venta
        public IActionResult VerDetalle(int id)
        {
            var detalleVenta = new VentaDetalleCompletoVM();

            try
            {
                var connectionString = _context.Database.GetConnectionString();

                using (var cn = new SqlConnection(connectionString))
                {
                    cn.Open();

                    using (var cmd = new SqlCommand("SP_ObtenerDetalleVenta", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id_Venta", id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            // 1️⃣ Leer cabecera de la venta
                            if (reader.Read())
                            {
                                detalleVenta.Id_Venta = reader.GetInt32("Id_Venta");
                                detalleVenta.Fecha_Venta = reader.GetDateTime("Fecha_Venta");
                                detalleVenta.Nombre_Cliente = reader.GetString("Nombre_Cliente");
                                detalleVenta.Nombre_Empleado = reader.GetString("Nombre_Empleado");
                                detalleVenta.Metodo_Pago = reader.GetString("Metodo_Pago");
                                detalleVenta.Tipo_Comprobante = reader.GetString("Tipo_Comprobante");
                                detalleVenta.Monto_Total = reader.GetDecimal("Monto_Total");
                            }

                            // 2️⃣ Leer detalles de la venta
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    var detalle = new VentaDetalleItemVM
                                    {
                                        Tipo_Item = reader.GetString("Tipo_Item"),
                                        Nombre_Item = reader.GetString("Nombre_Item"),
                                        Cantidad = reader.GetInt32("Cantidad"),
                                        Precio_Unitario = reader.GetDecimal("Precio_Unitario"),
                                        SubTotal = reader.GetDecimal("SubTotal")
                                    };
                                    detalleVenta.Detalles.Add(detalle);
                                }
                            }
                        }
                    }
                }

                if (detalleVenta.Id_Venta == 0)
                {
                    TempData["Error"] = "No se encontró la venta especificada";
                    return RedirectToAction("Index");
                }

                return View("_DetalleVenta", detalleVenta);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el detalle: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}