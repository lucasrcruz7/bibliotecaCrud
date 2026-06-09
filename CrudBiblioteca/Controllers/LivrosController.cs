using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudBiblioteca.Models;
using CrudBiblioteca.Data;

public class LivrosController : Controller
{
    private readonly AppDbContext _context;

    public LivrosController(AppDbContext context)
    {
        _context = context;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Livros
    // Lista todos os livros com busca por título, autor ou categoria
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? busca)
    {
        var query = _context.Livros.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim().ToLower();
            query = query.Where(l =>
                l.NomeLivro.ToLower().Contains(busca) ||
                l.Autor.ToLower().Contains(busca) ||
                l.Categoria.ToLower().Contains(busca));
        }

        ViewBag.Busca = busca;
        return View(await query.OrderBy(l => l.NomeLivro).ToListAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Livros/Details/5
    // Exibe detalhes + quantidade disponível em estoque
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var livro = await _context.Livros.FirstOrDefaultAsync(m => m.Id == id);

        if (livro == null)
            return NotFound();

        // Quantidade de empréstimos ativos deste livro
        ViewBag.EmprestimosAtivos = await _context.Emprestimos
            .CountAsync(e => e.LivroId == id &&
                             e.Status != StatusEmprestimo.Devolvido);

        return View(livro);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Livros/Create
    // ─────────────────────────────────────────────────────────────────────────
    public IActionResult Create()
    {
        return View();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Livros/Create
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Id,NomeLivro,Autor,QuantidadeEstoque,FaixaEtariaPermitida,Categoria,AnoPublicacao")]
        Livro livro)
    {
        // Validação: ano de publicação não pode ser futuro
        if (livro.AnoPublicacao > DateTime.Now.Year)
            ModelState.AddModelError("AnoPublicacao", "O ano de publicação não pode ser no futuro.");

        if (ModelState.IsValid)
        {
            _context.Add(livro);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = $"Livro \"{livro.NomeLivro}\" cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        return View(livro);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Livros/Edit/5
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var livro = await _context.Livros.FindAsync(id);

        if (livro == null)
            return NotFound();

        return View(livro);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Livros/Edit/5
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Id,NomeLivro,Autor,QuantidadeEstoque,FaixaEtariaPermitida,Categoria,AnoPublicacao")]
        Livro livro)
    {
        if (id != livro.Id)
            return NotFound();

        if (livro.AnoPublicacao > DateTime.Now.Year)
            ModelState.AddModelError("AnoPublicacao", "O ano de publicação não pode ser no futuro.");

        // Validação: estoque não pode ser menor que empréstimos ativos
        int emprestimosAtivos = await _context.Emprestimos
            .CountAsync(e => e.LivroId == livro.Id &&
                             e.Status != StatusEmprestimo.Devolvido);

        if (livro.QuantidadeEstoque < 0)
            ModelState.AddModelError("QuantidadeEstoque", "O estoque não pode ser negativo.");
        else if (livro.QuantidadeEstoque < emprestimosAtivos)
            ModelState.AddModelError("QuantidadeEstoque",
                $"Existem {emprestimosAtivos} empréstimo(s) ativo(s). O estoque não pode ser menor que isso.");

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(livro);
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = $"Livro \"{livro.NomeLivro}\" atualizado com sucesso!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LivroExists(livro.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(livro);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Livros/Delete/5
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var livro = await _context.Livros.FirstOrDefaultAsync(m => m.Id == id);

        if (livro == null)
            return NotFound();

        // Avisa se há empréstimos ativos antes de deletar
        ViewBag.EmprestimosAtivos = await _context.Emprestimos
            .CountAsync(e => e.LivroId == id &&
                             e.Status != StatusEmprestimo.Devolvido);

        return View(livro);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Livros/Delete/5
    // Bloqueia exclusão se houver empréstimos ativos
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var livro = await _context.Livros.FindAsync(id);

        if (livro == null)
            return NotFound();

        // Bloqueia se tiver QUALQUER empréstimo vinculado (ativo ou não)
        int emprestimosVinculados = await _context.Emprestimos
            .CountAsync(e => e.LivroId == id);

        if (emprestimosVinculados > 0)
        {
            TempData["Erro"] = $"Não é possível excluir \"{livro.NomeLivro}\" pois há {emprestimosVinculados} empréstimo(s) vinculado(s) ao histórico.";
            return RedirectToAction(nameof(Index));
        }

        _context.Livros.Remove(livro);
        await _context.SaveChangesAsync();
        TempData["Sucesso"] = $"Livro \"{livro.NomeLivro}\" excluído com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    private bool LivroExists(int? id)
    {
        return _context.Livros.Any(e => e.Id == id);
    }
}
