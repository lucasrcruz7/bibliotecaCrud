using CrudBiblioteca.Models;
using System.ComponentModel.DataAnnotations;
public class VendaLivro
{
    public int Id { get; set; }
    public DateTime DataVenda { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; }

    public int LivroId { get; set; }
    public Livro Livro { get; set; }

    public int Quantidade { get; set; }
    public decimal ValorTotal { get; set; }
}
