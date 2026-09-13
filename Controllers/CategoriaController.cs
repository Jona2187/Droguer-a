using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Drogueria.Data;
using Drogueria.Models;

namespace Drogueria.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly AppDbContex _context;

        public CategoriaController(AppDbContex context)
        {
            _context = context;
        }

        // GET: /Categoria
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .OrderByDescending(c => c.FechaCreacion)
                .ToListAsync();

            return View("~/Views/Home/pages/_Categorias.cshtml", categorias);
        }

        // POST: /Categoria/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Descripcion,Estado")] Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                categoria.Id = Guid.NewGuid();
                categoria.FechaCreacion = DateTime.Now;
                _context.Categorias.Add(categoria);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Categoría creada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error al crear la categoría.";
            var categorias = await _context.Categorias.OrderByDescending(c => c.FechaCreacion).ToListAsync();
            return View("~/Views/Home/pages/_Categorias.cshtml", categorias);
        }

        // POST: /Categoria/Edit/guid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Nombre,Descripcion,Estado,FechaCreacion")] Categoria categoria)
        {
            if (id != categoria.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Categorias.Update(categoria);
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Categoría actualizada correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categorias.Any(c => c.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Error al actualizar la categoría.";
            var categorias = await _context.Categorias.OrderByDescending(c => c.FechaCreacion).ToListAsync();
            return View("~/Views/Home/pages/_Categorias.cshtml", categorias);
        }

        // POST: /Categoria/Delete/guid  — soft delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria != null)
            {
                categoria.Estado = false;
                _context.Categorias.Update(categoria);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"La categoría '{categoria.Nombre}' fue desactivada correctamente.";
            }
            else
            {
                TempData["Error"] = "La categoría no fue encontrada.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Categoria/ToggleEstado/guid  — activa o desactiva según estado actual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEstado(Guid id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
                return NotFound();

            categoria.Estado = !categoria.Estado;
            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();

            var accion = categoria.Estado ? "activada" : "desactivada";
            TempData["Exito"] = $"La categoría '{categoria.Nombre}' fue {accion} correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
