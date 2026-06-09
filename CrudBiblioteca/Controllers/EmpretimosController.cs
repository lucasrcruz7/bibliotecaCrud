using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CrudBiblioteca.Models;
using CrudBiblioteca.Data;

public class EmprestimosController : Controller
{
    private readonly AppDbContext _context;

    // Valor da multa por dia de atraso (R$)
    private const decimal MultaPorDia = 1.00m;

    // Prazo padrão de devolução em dias
    private const int PrazoDevolucaoDias = 14;

    public EmprestimosController(AppDbContext context)
    {
        _context = context;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Emprestimos
    // Consultar histórico — lista todos com filtro opcional por usuário/status
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(int? usuarioId, StatusEmprestimo? status)
    {
        var query = _context.Emprestimos
            .Include(e => e.Usuario)
            .Include(e => e.Livro)
            .AsQueryable();

        if (usuarioId.HasValue)
            query = query.Where(e => e.UsuarioId == usuarioId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        // Atualiza automaticamente empréstimos atrasados antes de listar
        await AtualizarEmprestimosAtrasados();

        ViewBag.Usuarios = new SelectList(await _context.Usuarios.ToListAsync(), "Id", "NomeCompleto");
        ViewBag.StatusFiltro = status;
        ViewBag.UsuarioFiltro = usuarioId;

        return View(await query.OrderByDescending(e => e.DataEmprestimo).ToListAsync());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Emprestimos/Details/5
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var emprestimo = await _context.Emprestimos
            .Include(e => e.Usuario)
            .Include(e => e.Livro)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (emprestimo == null)
            return NotFound();

        return View(emprestimo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Emprestimos/Create
    // Registrar empréstimo — exibe formulário
    // ─────────────────────────────────────────────────────────────────────────
    public IActionResult Create()
    {
        // Apenas usuários ativos
        ViewBag.Usuarios = new SelectList(
            _context.Usuarios.Where(u => u.Status == StatusUsuario.Ativo),
            "Id", "NomeCompleto");

        // Apenas livros com estoque disponível
        ViewBag.Livros = new SelectList(
            _context.Livros.Where(l => l.QuantidadeEstoque > 0),
            "Id", "NomeLivro");

        return View();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Emprestimos/Create
    // Registrar empréstimo — aplica todas as regras de negócio
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("UsuarioId,LivroId")] Emprestimo emprestimo)
    {
        // Carrega entidades relacionadas para validação
        var usuario = await _context.Usuarios.FindAsync(emprestimo.UsuarioId);
        var livro = await _context.Livros.FindAsync(emprestimo.LivroId);

        // ── Validação: usuário existe e está ativo ───────────────────────────
        if (usuario == null || usuario.Status != StatusUsuario.Ativo)
        {
            ModelState.AddModelError("UsuarioId", "Usuário não encontrado ou inativo.");
        }

        // ── Validação: livro existe e tem estoque ────────────────────────────
        if (livro == null || livro.QuantidadeEstoque <= 0)
        {
            ModelState.AddModelError("LivroId", "Livro não disponível em estoque.");
        }

        // ── Validação: faixa etária ──────────────────────────────────────────
        if (usuario != null && livro != null)
        {
            int idadeUsuario = CalcularIdade(usuario.DataNascimento);
            int faixaMinima = (int)livro.FaixaEtariaPermitida;

            if (idadeUsuario < faixaMinima)
            {
                ModelState.AddModelError("LivroId",
                    $"Usuário não tem idade mínima para este livro (mínimo {faixaMinima} anos).");
            }
        }

        if (ModelState.IsValid)
        {
            // Preenche campos automáticos
            emprestimo.DataEmprestimo = DateTime.Now;
            emprestimo.DataPrevistaDevolucao = DateTime.Now.AddDays(PrazoDevolucaoDias);
            emprestimo.Status = StatusEmprestimo.Emprestado;
            emprestimo.Multa = 0;
            emprestimo.DataRealDevolucao = null;

            // Decrementa estoque
            livro!.QuantidadeEstoque--;
            _context.Update(livro);

            _context.Add(emprestimo);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Empréstimo registrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // Recarrega dropdowns em caso de erro
        ViewBag.Usuarios = new SelectList(
            _context.Usuarios.Where(u => u.Status == StatusUsuario.Ativo),
            "Id", "NomeCompleto", emprestimo.UsuarioId);

        ViewBag.Livros = new SelectList(
            _context.Livros.Where(l => l.QuantidadeEstoque > 0),
            "Id", "NomeLivro", emprestimo.LivroId);

        return View(emprestimo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Emprestimos/Devolver/5
    // Registrar devolução — exibe tela de confirmação com preview da multa
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Devolver(int? id)
    {
        if (id == null)
            return NotFound();

        var emprestimo = await _context.Emprestimos
            .Include(e => e.Usuario)
            .Include(e => e.Livro)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (emprestimo == null)
            return NotFound();

        if (emprestimo.Status == StatusEmprestimo.Devolvido)
        {
            TempData["Erro"] = "Este empréstimo já foi devolvido.";
            return RedirectToAction(nameof(Index));
        }

        // Preview da multa na tela
        ViewBag.MultaPreview = CalcularMulta(emprestimo.DataPrevistaDevolucao, DateTime.Now);
        ViewBag.DiasAtraso = CalcularDiasAtraso(emprestimo.DataPrevistaDevolucao, DateTime.Now);

        return View(emprestimo);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST: Emprestimos/Devolver/5
    // Registrar devolução — calcula atraso e aplica multa
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost, ActionName("Devolver")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevolverConfirmado(int id)
    {
        var emprestimo = await _context.Emprestimos
            .Include(e => e.Livro)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (emprestimo == null)
            return NotFound();

        if (emprestimo.Status == StatusEmprestimo.Devolvido)
        {
            TempData["Erro"] = "Este empréstimo já foi devolvido.";
            return RedirectToAction(nameof(Index));
        }

        DateTime dataDevolucao = DateTime.Now;

        // ── Calcula atraso e multa ───────────────────────────────────────────
        emprestimo.DataRealDevolucao = dataDevolucao;
        emprestimo.Multa = CalcularMulta(emprestimo.DataPrevistaDevolucao, dataDevolucao);

        int diasAtraso = CalcularDiasAtraso(emprestimo.DataPrevistaDevolucao, dataDevolucao);
        emprestimo.Status = diasAtraso > 0
            ? StatusEmprestimo.Atrasado
            : StatusEmprestimo.Devolvido;

        // ── Devolve livro ao estoque ─────────────────────────────────────────
        emprestimo.Livro!.QuantidadeEstoque++;
        _context.Update(emprestimo.Livro);

        _context.Update(emprestimo);
        await _context.SaveChangesAsync();

        string mensagem = diasAtraso > 0
            ? $"Devolução registrada com {diasAtraso} dia(s) de atraso. Multa: R$ {emprestimo.Multa:F2}"
            : "Devolução registrada no prazo. Sem multa!";

        TempData["Sucesso"] = mensagem;
        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET: Emprestimos/Historico/5
    // Histórico de empréstimos de um usuário específico
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Historico(int? id)
    {
        if (id == null)
            return NotFound();

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
            return NotFound();

        var historico = await _context.Emprestimos
            .Include(e => e.Livro)
            .Where(e => e.UsuarioId == id)
            .OrderByDescending(e => e.DataEmprestimo)
            .ToListAsync();

        ViewBag.Usuario = usuario;
        return View(historico);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MÉTODOS PRIVADOS — Regras de negócio
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula a multa com base nos dias de atraso.
    /// </summary>
    private static decimal CalcularMulta(DateTime dataPrevista, DateTime dataReal)
    {
        int dias = CalcularDiasAtraso(dataPrevista, dataReal);
        return dias > 0 ? dias * MultaPorDia : 0m;
    }

    /// <summary>
    /// Retorna quantos dias de atraso houve (0 se no prazo).
    /// </summary>
    private static int CalcularDiasAtraso(DateTime dataPrevista, DateTime dataReal)
    {
        int dias = (dataReal.Date - dataPrevista.Date).Days;
        return dias > 0 ? dias : 0;
    }

    /// <summary>
    /// Calcula idade em anos completos a partir da data de nascimento.
    /// </summary>
    private static int CalcularIdade(DateTime dataNascimento)
    {
        var hoje = DateTime.Today;
        int idade = hoje.Year - dataNascimento.Year;
        if (dataNascimento.Date > hoje.AddYears(-idade)) idade--;
        return idade;
    }

    /// <summary>
    /// Varre empréstimos "Emprestado" com prazo vencido e marca como "Atrasado".
    /// Chamado automaticamente ao listar.
    /// </summary>
    private async Task AtualizarEmprestimosAtrasados()
    {
        var atrasados = await _context.Emprestimos
            .Where(e => e.Status == StatusEmprestimo.Emprestado
                     && e.DataPrevistaDevolucao.Date < DateTime.Today)
            .ToListAsync();

        foreach (var e in atrasados)
        {
            e.Status = StatusEmprestimo.Atrasado;
            _context.Update(e);
        }

        if (atrasados.Any())
            await _context.SaveChangesAsync();
    }

    private bool EmprestimoExists(int id)
    {
        return _context.Emprestimos.Any(e => e.Id == id);
    }
}