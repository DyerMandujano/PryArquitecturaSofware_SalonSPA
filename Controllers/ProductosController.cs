using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pry_Solu_SalonSPA.Db;
using Pry_Solu_SalonSPA.Models;
using System.Linq;

namespace Pry_Solu_SalonSPA.Controllers
{
    public class ProductosController : Controller
    {
        private readonly Conexion _context;

        public ProductosController(Conexion context)
        {
            _context = context;
        }

        // =============================================================
        // LISTAR PRODUCTOS
        // =============================================================
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .FromSqlRaw("EXEC SP_ListarProductos")
                .ToListAsync();

            return View(productos);
        }

        // =============================================================
        // CREAR PRODUCTO (GET)
        // =============================================================
        public IActionResult Crear()
        {
            CargarCombos();
            return View("_CrearProducto", new Producto());
        }

        // =============================================================
        // CREAR PRODUCTO (POST)
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Producto producto)
        {
            if (!ModelState.IsValid)
            {
                CargarCombos();
                return View("_CrearProducto", producto);
            }

            var paramCat = new SqlParameter("@id_categoria", producto.IdCategoria);
            var paramMarca = new SqlParameter("@id_marca", producto.IdMarca);
            var paramNom = new SqlParameter("@nom_prod", producto.NomProd);
            var paramStock = new SqlParameter("@stock", producto.Stock);
            var paramPrecio = new SqlParameter("@precio", producto.Precio);
            var paramDesc = new SqlParameter("@descripcion", producto.Descripcion);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC SP_InsertProducto @id_categoria, @id_marca, @nom_prod, @stock, @precio, @descripcion",
                paramCat, paramMarca, paramNom, paramStock, paramPrecio, paramDesc
            );

            return RedirectToAction(nameof(Index));
        }

        // =============================================================
        // EDITAR PRODUCTO (GET)
        // =============================================================
        public async Task<IActionResult> Editar(int id)
        {
            var producto = await _context.Productos
                .FromSqlRaw("SELECT * FROM Producto WHERE Id_Producto = {0}", id)
                .FirstOrDefaultAsync();

            if (producto == null)
                return NotFound();

            ViewBag.IdProducto = id;

            CargarCombos();
            return View("_EditarProducto", producto);
        }

        // =============================================================
        // EDITAR PRODUCTO (POST)
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Producto producto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IdProducto = id;
                CargarCombos();
                return View("_EditarProducto", producto);
            }

            var paramId = new SqlParameter("@id_producto", id);
            var paramCat = new SqlParameter("@id_categoria", producto.IdCategoria);
            var paramMarca = new SqlParameter("@id_marca", producto.IdMarca);
            var paramNom = new SqlParameter("@nom_prod", producto.NomProd);
            var paramStock = new SqlParameter("@stock", producto.Stock);
            var paramPrecio = new SqlParameter("@precio", producto.Precio);
            var paramDesc = new SqlParameter("@descripcion", producto.Descripcion);
            var paramEstado = new SqlParameter("@estado", producto.Estado);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC SP_UpdateProducto @id_producto, @id_categoria, @id_marca, @nom_prod, @stock, @precio, @descripcion, @estado",
                paramId, paramCat, paramMarca, paramNom, paramStock, paramPrecio, paramDesc, paramEstado
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
            var producto = await _context.Productos
                .FromSqlRaw("SELECT * FROM Producto WHERE Id_Producto = {0}", id)
                .FirstOrDefaultAsync();

            if (producto == null)
                return NotFound();

            var paramId = new SqlParameter("@id_producto", id);

            if (producto.Estado == 1)
            {
                // DESACTIVAR (SP_DeleteProducto)
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC SP_DeleteProducto @id_producto",
                    paramId
                );
            }
            else
            {
                // ACTIVAR
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Producto SET Estado = 1 WHERE Id_Producto = @id_producto",
                    paramId
                );
            }

            return RedirectToAction(nameof(Index));
        }

        // =============================================================
        // MÉTODO PARA LLENAR COMBOS DE CATEGORÍA Y MARCA
        // =============================================================
        private void CargarCombos()
        {
            ViewBag.Categorias = 
                _context.Categorias.FromSqlRaw("EXEC SP_ListarCategorias").ToList();


            ViewBag.Marcas = _context.Marcas
                .FromSqlRaw("EXEC SP_ListarMarcas")
                .ToList();

            Console.WriteLine("Categorias encontradas: " + ViewBag.Categorias.Count);
            Console.WriteLine("Marcas encontradas: " + ViewBag.Marcas.Count);

        }
    }
}
