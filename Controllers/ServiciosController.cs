using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pry_Solu_SalonSPA.Db;
using Pry_Solu_SalonSPA.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pry_Solu_SalonSPA.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly Conexion _context;

        public ServiciosController(Conexion context)
        {
            _context = context;
        }

        // GET: Servicios/Index
        public IActionResult Index(string filtro)
        {
            List<Servicio> servicios;

            if (string.IsNullOrEmpty(filtro))
            {
                servicios = _context.Servicios
                    .FromSqlRaw("EXEC SP_Listar_Servicios")
                    .AsEnumerable()
                    .ToList();
            }
            else
            {
                var parametro = new SqlParameter("@Criterio", filtro);
                servicios = _context.Servicios
                    .FromSqlRaw("EXEC SP_Buscar_Servicios_PorId @Criterio", parametro)
                    .AsEnumerable()
                    .ToList();
            }

            foreach (var servicio in servicios)
            {
                servicio.IdTipoServicioNavigation = _context.TipoServicios
                    .FirstOrDefault(ts => ts.IdTipoServicio == servicio.IdTipoServicio);
            }

            return View(servicios);
        }

        // GET: Servicios/FiltrarPorEstado
        public IActionResult FiltrarPorEstado(int estado)
        {
            var parametro = new SqlParameter("@Estado", estado);
            var servicios = _context.Servicios
                .FromSqlRaw("EXEC SP_Filtrar_Servicios_PorEstado @Estado", parametro)
                .AsEnumerable()
                .ToList();

            foreach (var servicio in servicios)
            {
                servicio.IdTipoServicioNavigation = _context.TipoServicios
                    .FirstOrDefault(ts => ts.IdTipoServicio == servicio.IdTipoServicio);
            }

            return View("Index", servicios);
        }

        // GET: Servicios/Crear
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarTiposServicio();
            return View("CrearServicios", new Servicio());
        }

        // POST: Servicios/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Servicio servicio)
        {
            if (servicio.IdTipoServicio == 0)
            {
                TempData["Error"] = "Debe seleccionar un tipo de servicio";
                await CargarTiposServicio();
                return View("CrearServicios", servicio);
            }

            if (!ModelState.IsValid)
            {
                await CargarTiposServicio();
                return View("CrearServicios", servicio);
            }

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Nombre", servicio.Nombre ?? ""),
                    new SqlParameter("@Descripcion", servicio.Descripcion ?? ""),
                    new SqlParameter("@Precio", servicio.Precio),
                    new SqlParameter("@Duracion", servicio.Duracion),
                    new SqlParameter("@Id_TipoServicio", servicio.IdTipoServicio),
                    new SqlParameter("@Estado", 1)
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_Registrar_Servicio @Nombre, @Descripcion, @Precio, @Duracion, @Id_TipoServicio, @Estado",
                    parameters);

                TempData["Mensaje"] = "Servicio creado correctamente";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException sqlEx)
            {
                TempData["Error"] = $"Error de base de datos: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear el servicio: {ex.Message}";
            }

            await CargarTiposServicio();
            return View("CrearServicios", servicio);
        }

        // GET: Servicios/Editar
        [HttpGet]
        public IActionResult Editar(int id)
        {
            var servicio = _context.Servicios.Find(id);
            if (servicio == null)
                return NotFound();

            var tiposServicio = _context.TipoServicios.Where(ts => ts.Estado == 1).ToList();
            ViewBag.TiposServicio = new SelectList(tiposServicio, "IdTipoServicio", "Descripcion");

            return View("_EditarServicios", servicio);
        }

        // POST: Servicios/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Servicio servicio)
        {
            if (ModelState.IsValid)
            {
                var parametros = new[]
                {
                    new SqlParameter("@IdServicio", id),
                    new SqlParameter("@Nombre", servicio.Nombre),
                    new SqlParameter("@Descripcion", servicio.Descripcion),
                    new SqlParameter("@Precio", servicio.Precio),
                    new SqlParameter("@Duracion", servicio.Duracion),
                    new SqlParameter("@Id_TipoServicio", servicio.IdTipoServicio),
                    new SqlParameter("@Estado", servicio.Estado)
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_Modificar_Servicio @IdServicio, @Nombre, @Descripcion, @Precio, @Duracion, @Id_TipoServicio, @Estado",
                    parametros);

                TempData["Mensaje"] = "Servicio actualizado correctamente";
                return RedirectToAction(nameof(Index));
            }

            var tiposServicio = _context.TipoServicios
                .Where(ts => ts.Estado == 1)
                .ToList();

            ViewBag.TiposServicio = new SelectList(tiposServicio, "IdTipoServicio", "Descripcion", servicio.IdTipoServicio);

            return View("_EditarServicios", servicio);
        }

        // POST: Servicios/Eliminar
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var parametro = new SqlParameter("@IdServicio", id);
            await _context.Database.ExecuteSqlRawAsync("EXEC SP_Eliminar_Servicio @IdServicio", parametro);

            TempData["Mensaje"] = "Servicio eliminado correctamente";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarTiposServicio()
        {
            var tiposServicio = await _context.TipoServicios
                .Where(ts => ts.Estado == 1)
                .ToListAsync();

            ViewBag.TiposServicio = new SelectList(tiposServicio, "IdTipoServicio", "Descripcion");
        }
    }
}