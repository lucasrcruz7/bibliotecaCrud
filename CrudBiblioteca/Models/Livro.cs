using System.ComponentModel.DataAnnotations;

namespace CrudBiblioteca.Models
{
    public class Livro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string NomeLivro { get; set; }

        [Required]
        [StringLength(150)]
        public string Autor { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int QuantidadeEstoque { get; set; }

        [Required]
        public FaixaEtaria FaixaEtariaPermitida { get; set; }

        [Required]
        [StringLength(100)]
        public string Categoria { get; set; }

        [Required]
        [Range(1000, 9999)]
        public int AnoPublicacao { get; set; }
    }

    public enum FaixaEtaria
    {
        Livre = 0,
        Anos10 = 10,
        Anos12 = 12,
        Anos14 = 14,
        Anos16 = 16,
        Anos18 = 18
    }
}
