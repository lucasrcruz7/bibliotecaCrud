using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudBiblioteca.Models;
using CrudBiblioteca.Data;

public class UsuariosController : Controller
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Usuarios
    // Lista com busca por nome, e-mail ou status
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? busca, StatusUsuario? status)
    {
        var query = _context.Usuarios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim().ToLower();
            query = query.Where(u =>
                u.NomeCompleto.ToLower().Contains(busca) ||
                u.Email.ToLower().Contains(busca));
        }

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        ViewBag.Busca = busca;
        ViewBag.StatusFiltro = status;

        return View(await query.OrderBy(u => u.NomeCompleto).ToListAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Usuarios/Details/5
    // Exibe dados do usuário + histórico resumido de empréstimos
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.Id == id);

        if (usuario == null)
            return NotFound();

        ViewBag.TotalEmprestimos = await _context.Emprestimos
            .CountAsync(e => e.UsuarioId == id);

        ViewBag.EmprestimosAtivos = await _context.Emprestimos
            .CountAsync(e => e.UsuarioId == id &&
                             e.Status != StatusEmprestimo.Devolvido);

        ViewBag.TotalMultas = await _context.Emprestimos
            .Where(e => e.UsuarioId == id)
            .SumAsync(e => e.Multa);

        return View(usuario);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Usuarios/Create
    // ─────────────────────────────────────────────────────────────────────────
    public IActionResult Create()
    {
        return View();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Usuarios/Create
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Id,NomeCompleto,DataNascimento,Email,Senha,Status")]
        Usuario usuario)
    {
        // Validação: e-mail duplicado
        if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            ModelState.AddModelError("Email", "Este e-mail já está cadastrado.");

        // Validação: data de nascimento não pode ser futura
        if (usuario.DataNascimento > DateTime.Today)
            ModelState.AddModelError("DataNascimento", "A data de nascimento não pode ser no futuro.");

        if (ModelState.IsValid)
        {
            _context.Add(usuario);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = $"Usuário \"{usuario.NomeCompleto}\" cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        return View(usuario);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Usuarios/Edit/5
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return NotFound();

        return View(usuario);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Usuarios/Edit/5
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Id,NomeCompleto,DataNascimento,Email,Senha,Status")]
        Usuario usuario)
    {
        if (id != usuario.Id)
            return NotFound();

        // Validação: e-mail duplicado (ignora o próprio usuário)
        if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email && u.Id != usuario.Id))
            ModelState.AddModelError("Email", "Este e-mail já está em uso por outro usuário.");

        // Validação: data de nascimento não pode ser futura
        if (usuario.DataNascimento > DateTime.Today)
            ModelState.AddModelError("DataNascimento", "A data de nascimento não pode ser no futuro.");

        // Bloqueia inativação se houver empréstimos ativos
        if (usuario.Status == StatusUsuario.Inativo)
        {
            int ativos = await _context.Emprestimos
                .CountAsync(e => e.UsuarioId == usuario.Id &&
                                 e.Status != StatusEmprestimo.Devolvido);

            if (ativos > 0)
                ModelState.AddModelError("Status",
                    $"Não é possível inativar o usuário pois há {ativos} empréstimo(s) ativo(s).");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(usuario);
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = $"Usuário \"{usuario.NomeCompleto}\" atualizado com sucesso!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(usuario.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(usuario);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Usuarios/Delete/5
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.Id == id);

        if (usuario == null)
            return NotFound();

        ViewBag.EmprestimosAtivos = await _context.Emprestimos
            .CountAsync(e => e.UsuarioId == id &&
                             e.Status != StatusEmprestimo.Devolvido);

        return View(usuario);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Usuarios/Delete/5
    // Bloqueia exclusão se houver empréstimos ativos
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return NotFound();

        int emprestimosAtivos = await _context.Emprestimos
            .CountAsync(e => e.UsuarioId == id &&
                             e.Status != StatusEmprestimo.Devolvido);

        if (emprestimosAtivos > 0)
        {
            TempData["Erro"] = $"Não é possível excluir \"{usuario.NomeCompleto}\" pois há {emprestimosAtivos} empréstimo(s) ativo(s).";
            return RedirectToAction(nameof(Index));
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        TempData["Sucesso"] = $"Usuário \"{usuario.NomeCompleto}\" excluído com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Usuarios/Login
    // ─────────────────────────────────────────────────────────────────────────
    public IActionResult Login()
    {
        return View();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Usuarios/Login
    // Login simples por e-mail e senha
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            ViewBag.Erro = "Preencha e-mail e senha.";
            return View();
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Senha == senha);

        if (usuario == null)
        {
            ViewBag.Erro = "E-mail ou senha incorretos.";
            return View();
        }

        if (usuario.Status == StatusUsuario.Inativo)
        {
            ViewBag.Erro = "Usuário inativo. Entre em contato com a biblioteca.";
            return View();
        }

        // Armazena nome na sessão (simples, sem Identity)
        HttpContext.Session.SetString("UsuarioNome", usuario.NomeCompleto);
        HttpContext.Session.SetInt32("UsuarioId", usuario.Id);

        TempData["Sucesso"] = $"Bem-vindo, {usuario.NomeCompleto}!";
        return RedirectToAction("Index", "Home");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Usuarios/Logout
    // ─────────────────────────────────────────────────────────────────────────
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Sucesso"] = "Sessão encerrada com sucesso.";
        return RedirectToAction(nameof(Login));
    }

    private bool UsuarioExists(int? id)
    {
        return _context.Usuarios.Any(e => e.Id == id);
    }
}