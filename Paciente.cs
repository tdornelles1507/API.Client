using System;

namespace HMed.Api.MV.Models
{
    public class Paciente
    {
        public long? IdPaciente { get; set; }
        public long? IdAtendimento { get; set; }
        public string Nome { get; set; }
        public string NomeSocial { get; set; }
        public string Leito { get; set; }
        public string Responsavel { get; set; }
        public string SexoS { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string CPF { get; set; }
        public DateTime? DataAtendimento { get; set; }
        public string TipoAtendimento { get; set; }
        public string Telefone { get; set; }
        public string Convenio { get; set; }
        public string TipoSanguineo { get; set; }
        public string UnidadeAtendimento { get; set; }
        public string Categoria { get; set; }
        public string CorBoxS { get; set; }
        public string Tipo { get; set; }
        public int Ordem { get; set; }
        public string IdCID { get; set; }
        public string CID { get; set; }
        public string MedicoAssistente { get; set; }
        public string Email { get; set; }
        public string Endereco { get; set; }
    }
}