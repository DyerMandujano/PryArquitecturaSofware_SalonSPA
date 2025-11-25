using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pry_Solu_SalonSPA.Db;
using Pry_Solu_SalonSPA.Models;
using System.Linq;

namespace Pry_Solu_SalonSPA.Controllers
{
    public class MarcasController : Controller
    {
        private readonly Conexion _context;
        public MarcasController(Conexion context)
        {
            _context = context;
        }

        //LISTAR MARCAS
        public async Task<IActionResult> Index()
        {
            var marcas = await _context.Marcas
                .FromSqlRaw("EXEC SP_ListarMarcas")
                .ToListAsync();

            return View(marcas);
        }


        //INSERT MARCA
        public IActionResult Crear()
        {
            return View("_CrearMarca", new Marca());
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Marca marca)
        {
            if (!ModelState.IsValid)
            {
                return View("_CrearMarca", marca);
            }

            var paramNom = new SqlParameter("@nom_marca", marca.NomMarca);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC SP_InsertMarca @nom_marca",
                paramNom
            );

            return RedirectToAction(nameof(Index));
        }


        //UPDATE MARCA
        public async Task<IActionResult> Editar(int id)
        {
            var marca = await _context.Marcas
                .FromSqlRaw("SELECT * FROM Marca WHERE Id_Marca = {0}", id)
                .FirstOrDefaultAsync();

            if (marca == null)
                return NotFound();

            ViewBag.IdMarca = id;

            return View("_EditarMarca", marca);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Marca marca)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IdMarca = id;
                return View("_EditarMarca", marca);
            }

            var paramId = new SqlParameter("@id_marca", id);
            var paramNom = new SqlParameter("@nom_marca", marca.NomMarca);
            var paramEstado = new SqlParameter("@estado", marca.Estado);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC SP_UpdateMarca @id_marca, @nom_marca, @estado",
                paramId, paramNom, paramEstado
            );

            return RedirectToAction(nameof(Index));
        }

        //DELETE MARCA (CAMBIAR ESTADO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {

            var marca = await _context.Marcas
                .FromSqlRaw("SELECT * FROM Marca WHERE Id_Marca = {0}", id)
                .FirstOrDefaultAsync();

            if (marca == null)
                return NotFound();


            if (marca.Estado == 1)
            {
                var paramId = new SqlParameter("@id_marca", id);
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_DeleteMarca @id_marca",
                    paramId
                );
            }
            else
            {
                var paramId = new SqlParameter("@id_marca", id);

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Marca SET Estado = 1 WHERE Id_Marca = @id_marca",
                    paramId
                );
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
