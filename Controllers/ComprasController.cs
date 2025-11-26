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
    public class ComprasController : Controller
    {
        private readonly Conexion _context;

        public ComprasController(Conexion context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(string tipoBusqueda = "TODOS", string valorBusqueda = "")
        {
            List<CompraListaVM> lista = _context.BuscarCompras(tipoBusqueda, valorBusqueda);

            return View(lista);
        }

        [HttpGet]
        public IActionResult Detalle(int id)
        {
            var compra = _context.BuscarCompras("ID_COMPRA", id.ToString());

            if (compra == null || compra.Count == 0)
                return RedirectToAction("Index");

            return View(compra[0]);
        }


        [HttpGet]
        public IActionResult Registrar()
        {
            var model = new CompraRegistrarVM
            {
                FechaCompra = DateTime.Now,
                Proveedor = _context.Proveedor.ToList(),
                Productos = _context.Productos.ToList(),
                Detalles = new List<CompraDetalleVM>()     // inicializar vacío
            };

            return View("_RegistrarCompras", model);
        }

        [HttpPost]
        public IActionResult Registrar(CompraRegistrarVM model)
        {
            // ==========================
            // DEBUG 1: Datos recibidos
            // ==========================
            Console.WriteLine("=========== DEBUG: POST REGISTRAR ===========");
            Console.WriteLine($"IdProveedor: {model.IdProveedor}");
            Console.WriteLine($"TipoDoc: {model.TipoDoc}");
            Console.WriteLine($"FechaCompra: {model.FechaCompra}");
            Console.WriteLine($"Detalles count: {model.Detalles?.Count ?? 0}");

            if (model.Detalles != null)
            {
                foreach (var d in model.Detalles)
                {
                    Console.WriteLine($" -> Producto {d.IdProducto}, Cant {d.Cantidad}, Precio {d.PrecioCompra}");
                }
            }
            Console.WriteLine("==============================================");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("==> ModelState inválido");
                model.Proveedor = _context.Proveedor.ToList();
                model.Productos = _context.Productos.ToList();
                return View("_RegistrarCompras", model);
            }

            if (model.Detalles == null || !model.Detalles.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto.");

                Console.WriteLine("==> ERROR: No llegaron detalles");

                model.Proveedor = _context.Proveedor.ToList();
                model.Productos = _context.Productos.ToList();
                return View("_RegistrarCompras", model);
            }

            var connectionString = _context.Database.GetConnectionString();
            Console.WriteLine($"Connection string: {connectionString}");

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        int idCompra;

                        // ==================================================
                        // DEBUG 2: Insertar compra
                        // ==================================================
                        Console.WriteLine("==> INSERTANDO COMPRA...");

                        using (var cmd = new SqlCommand("SP_InsertarCompra", cn, tx))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Id_Proveedor", model.IdProveedor);
                            cmd.Parameters.AddWithValue("@Tipo_Doc", model.TipoDoc);
                            cmd.Parameters.AddWithValue("@Fecha_compra", model.FechaCompra);

                            var outId = new SqlParameter("@Id_Compra", SqlDbType.Int)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(outId);

                            cmd.ExecuteNonQuery();
                            idCompra = (int)outId.Value;

                            Console.WriteLine($" --> ID COMPRA GENERADO: {idCompra}");
                        }

                        // ==================================================
                        // DEBUG 3: Insertar detalles
                        // ==================================================
                        Console.WriteLine("==> INSERTANDO DETALLES...");

                        foreach (var det in model.Detalles)
                        {
                            Console.WriteLine($"   Detalle: Prod={det.IdProducto}, Cant={det.Cantidad}, Precio={det.PrecioCompra}");

                            using (var cmd = new SqlCommand("SP_InsertarDetalleCompra", cn, tx))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Id_Compra", idCompra);
                                cmd.Parameters.AddWithValue("@Id_Producto", det.IdProducto);
                                cmd.Parameters.AddWithValue("@Cantidad", det.Cantidad);
                                cmd.Parameters.AddWithValue("@Precio_Compra", det.PrecioCompra);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        Console.WriteLine("==> TRANSACCIÓN CONFIRMADA (COMMIT)");
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine("==> ERROR EN TRANSACCIÓN");
                        Console.WriteLine(ex.ToString());

                        ModelState.AddModelError("", "Error al registrar la compra: " + ex.Message);

                        model.Proveedor = _context.Proveedor.ToList();
                        model.Productos = _context.Productos.ToList();
                        return View("_RegistrarCompras", model);
                    }
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult DetallePorCompra(int id)
        {
            var model = new CompraDetallePorCompraVM
            {
                DetallesItem = new List<CompraDetalleItemVM>()
            };

            using (var conn = _context.Database.GetDbConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SP_GetDetalleCompra";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@Id_Compra", id));

                    using (var reader = cmd.ExecuteReader())
                    {
                        // 1️⃣ Datos principales de la compra
                        if (reader.Read())
                        {
                            model.IdCompra = reader.GetInt32(reader.GetOrdinal("Id_Compra"));
                            model.FechaCompra = reader.GetDateTime(reader.GetOrdinal("Fecha_compra"));
                            model.Proveedor = reader.GetString(reader.GetOrdinal("Nom_Prove"));
                            model.TipoDoc = reader.GetString(reader.GetOrdinal("Tipo_Doc"));
                        }

                        // 2️⃣ Segundo resultset: detalles de la compra
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var det = new CompraDetalleItemVM
                                {
                                    IdProducto = reader.GetInt32(reader.GetOrdinal("Id_Producto")),
                                    Producto = reader.GetString(reader.GetOrdinal("Nom_Prod")),
                                    Cantidad = reader.GetInt32(reader.GetOrdinal("Cantidad")),
                                    PrecioCompra = reader.GetDecimal(reader.GetOrdinal("Precio_Compra"))
                                };

                                model.DetallesItem.Add(det);
                            }
                        }
                    }
                }
            }

            // 3️⃣ Calcular monto total sumando los subtotales de cada detalle
            model.MontoTotal = model.DetallesItem.Sum(d => d.SubTotal);

            return View("_DetalleCompra", model);
        }








    }
}
