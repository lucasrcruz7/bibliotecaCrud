using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudBiblioteca.Models
{
    public class Emprestimo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime DataEmprestimo { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        [Required]
        public int LivroId { get; set; }

        [ForeignKey("LivroId")]
        public Livro Livro { get; set; }

        [Required]
        public DateTime DataPrevistaDevolucao { get; set; }

        public DateTime? DataRealDevolucao { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Multa { get; set; }

        [Required]
        public StatusEmprestimo Status { get; set; }
    }

    public enum StatusEmprestimo
    {
        Emprestado = 1,
        Devolvido = 2,
        Atrasado = 3
    }
}
