using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pry_Solu_SalonSPA.Db;
using Pry_Solu_SalonSPA.Models;

namespace Pry_Solu_SalonSPA.Controllers
{
    public class CategoriasController : Controller
    {
        private readonly Conexion _context;

        public CategoriasController(Conexion context)
        {
            _context = context;
        }

        // =============================================================
        // LISTAR CATEGORÍAS
        // =============================================================
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categoria
                .FromSqlRaw("EXEC SP_ListarCategorias")
                .ToListAsync();

            return View(categorias);
        }

        // =============================================================
        // CREAR CATEGORÍA (GET)
        // =============================================================
        public IActionResult Crear()
        {
            return View("_CrearCategoria", new Categoria());
        }

        // =============================================================
        // CREAR CATEGORÍA (POST)
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Categoria categoria)
        {
            if (!ModelState.IsValid)
            {
                return View("_CrearCategoria", categoria);
            }

            var paramNom = new SqlParameter("@nom_cate", categoria.NomCate);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC SP_InsertCategoria @nom_cate",
                paramNom
            );

            return RedirectToAction(nameof(Index));
        }

        // =============================================================
        // EDITAR CATEGORÍA (GET)
        // =============================================================
        public async Task<IActionResult> Editar(int id)
        {
            var categoria = await _context.Categoria
                .FromSqlRaw("SELECT * FROM Categoria WHERE Id_Categoria = {0}", id)
                .FirstOrDefaultAsync();

            if (categoria == null)
                return NotFound();

            ViewBag.IdCategoria = id;

            return View("_EditarCategoria", categoria);
        }

        // =============================================================
        // EDITAR CATEGORÍA (POST)
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Categoria categoria)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IdCategoria = id;
                return View("_EditarCategoria", categoria);
            }

            var paramId = new SqlParameter("@id_categoria", id);
            var paramNom = new SqlParameter("@nom_cate", categoria.NomCate);
            var paramEstado = new SqlParameter("@estado", categoria.Estado);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC SP_UpdateCategoria @id_categoria, @nom_cate, @estado",
                paramId, paramNom, paramEstado
            );

            return RedirectToAction(nameof(Index));
        }

        // =============================================================
        // CAMBIAR ESTADO (ACTIVAR / DESACTIVAR)
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var categoria = await _context.Categoria
                .FromSqlRaw("SELECT * FROM Categoria WHERE Id_Categoria = {0}", id)
                .FirstOrDefaultAsync();

            if (categoria == null)
                return NotFound();

            var paramId = new SqlParameter("@id_categoria", id);

            if (categoria.Estado == 1)
            {
                // DESACTIVAR (SP_DeleteCategoria)
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_DeleteCategoria @id_categoria",
                    paramId
                );
            }
            else
            {
                // ACTIVAR (Update directo)
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Categoria SET Estado = 1 WHERE Id_Categoria = @id_categoria",
                    paramId
                );
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
