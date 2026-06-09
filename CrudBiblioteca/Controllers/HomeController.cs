using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudBiblioteca.Data;
using CrudBiblioteca.Models;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Se não estiver logado, exibe a landing page pública
        bool logado = HttpContext.Session.GetInt32("UsuarioId").HasValue;

        if (!logado)
            return View("Landing");

        // Dashboard — dados para o usuário logado
        ViewBag.TotalLivros = await _context.Livros.CountAsync();
        ViewBag.LivrosDisponiveis = await _context.Livros.CountAsync(l => l.QuantidadeEstoque > 0);
        ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
        ViewBag.UsuariosAtivos = await _context.Usuarios.CountAsync(u => u.Status == StatusUsuario.Ativo);
        ViewBag.TotalEmprestimos = await _context.Emprestimos.CountAsync();
        ViewBag.EmprestimosAtivos = await _context.Emprestimos
            .CountAsync(e => e.Status == StatusEmprestimo.Emprestado);
        ViewBag.EmprestimosAtrasados = await _context.Emprestimos
            .CountAsync(e => e.Status == StatusEmprestimo.Atrasado);

        // Últimos 5 empréstimos
        ViewBag.UltimosEmprestimos = await _context.Emprestimos
            .Include(e => e.Usuario)
            .Include(e => e.Livro)
            .OrderByDescending(e => e.DataEmprestimo)
            .Take(5)
            .ToListAsync();

        return View();
    }
}