using System.ComponentModel.DataAnnotations;

namespace CrudBiblioteca.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string NomeCompleto { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DataNascimento { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string Senha { get; set; }

        [Required]
        public StatusUsuario Status { get; set; }
    }

    public enum StatusUsuario
    {
        Inativo = 0,
        Ativo = 1
    }
}
