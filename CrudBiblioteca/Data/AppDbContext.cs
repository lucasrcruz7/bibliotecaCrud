using Microsoft.EntityFrameworkCore;
using CrudBiblioteca.Models;

namespace CrudBiblioteca.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Livro> Livros { get; set; }
        public DbSet<Emprestimo> Emprestimos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Usuario ──────────────────────────────────────────────────────
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.NomeCompleto)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(u => u.DataNascimento)
                      .IsRequired();

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.Property(u => u.Senha)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(u => u.Status)
                      .IsRequired()
                      .HasConversion<int>();
            });

            // ── Livro ────────────────────────────────────────────────────────
            modelBuilder.Entity<Livro>(entity =>
            {
                entity.ToTable("Livros");
                entity.HasKey(l => l.Id);

                entity.Property(l => l.NomeLivro)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(l => l.Autor)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(l => l.QuantidadeEstoque)
                      .IsRequired()
                      .HasDefaultValue(0);

                entity.Property(l => l.FaixaEtariaPermitida)
                      .IsRequired()
                      .HasConversion<int>();

                entity.Property(l => l.Categoria)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(l => l.AnoPublicacao)
                      .IsRequired();
            });

            // ── Emprestimo ───────────────────────────────────────────────────
            modelBuilder.Entity<Emprestimo>(entity =>
            {
                entity.ToTable("Emprestimos");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DataEmprestimo)
                      .IsRequired();

                entity.Property(e => e.DataPrevistaDevolucao)
                      .IsRequired();

                entity.Property(e => e.DataRealDevolucao)
                      .IsRequired(false);

                entity.Property(e => e.Multa)
                      .HasColumnType("decimal(10,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.Status)
                      .IsRequired()
                      .HasConversion<int>();

                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Livro)
                      .WithMany()
                      .HasForeignKey(e => e.LivroId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}