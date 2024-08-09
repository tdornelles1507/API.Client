using Dapper;
using HMed.Api.MV.Helper;
using HMed.Api.MV.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HMed.Api.MV.Controllers
{
    [Route("api/[controller]")]
    [Authorize()]
    public class PacienteController : BaseController
    {
        public PacienteController(IConfiguration configuration)
        { _configuration = configuration; }

        [HttpGet]
        [Route("Internado/Count")]
        public async Task<IActionResult> GetCount(bool mostraTodosPacientes = false, string Pesquisa = "", bool mostraTodosPacientesPesquisa = false)
        {
            int total = 0;

            using (var _db = new OracleConnection(_configuration["Banco"]))
            {
                var idPrestador = User.Identity.GetPrestadorId();
                var strQuery = string.Empty;
                await _db.OpenAsync();
                string idPrestadorFiltro = BuscaPrestadorFiltro(mostraTodosPacientes, mostraTodosPacientesPesquisa, idPrestador.ToString(), Pesquisa);

                strQuery = $@" SELECT COUNT(distinct IdPaciente) TOTAL FROM TABLE({OwnerBanco}fnc_hmed_lista_internados({idPrestadorFiltro}, 'N', {idPrestador}))";

                strQuery += BuscaFiltrosQuery(idPrestadorFiltro, Pesquisa);

                total = (await _db.QueryAsync<int>(strQuery)).FirstOrDefault();
            }
            return Ok(total);
        }

        [HttpGet]
        [Route("Internado")]
        public async Task<IActionResult> Get(int inicio, int quantidade, string pesquisa = "", bool mostraTodosPacientes = false, bool mostraTodosPacientesPesquisa = false)
        {
            return Ok(await BuscaPacientesInternados(inicio, quantidade, pesquisa, mostraTodosPacientes, mostraTodosPacientesPesquisa));
        }

        [HttpGet]
        [Route("Meus")]
        public async Task<IActionResult> Meus(int inicio, int quantidade, string Pesquisa = "", bool mostraTodosPacientesPesquisa = true, bool filtrarPorPrestador = true)
        {
            return Ok(await BuscaMeusPacientes(inicio, quantidade, Pesquisa, mostraTodosPacientesPesquisa, !filtrarPorPrestador));
        }

        [HttpGet]
        [Route("Meus/Count")]
        public async Task<int> MeusCount(string Pesquisa = "", bool mostraTodosPacientesPesquisa = false)
        {
            int total = 0;
            long idPrestador = User.Identity.GetPrestadorId();
            string idPrestadorFiltro = BuscaPrestadorFiltro(false, mostraTodosPacientesPesquisa, idPrestador.ToString(), Pesquisa, true);
            using (var _db = new OracleConnection(_configuration["Banco"]))
            {
                await _db.OpenAsync();
                var strQuery = $@"SELECT COUNT(DISTINCT IdPaciente)
                                    FROM {OwnerBanco}VHMED_LISTA_PACIENTES ";
                strQuery += idPrestadorFiltro;
                strQuery += BuscaFiltrosQuery(idPrestadorFiltro, Pesquisa, isMeuPaciente: true);

                total = (await _db.QueryAsync<int>(strQuery)).FirstOrDefault();
            }
            return total;
        }

        [HttpPost]
        [Route("Favorito")]
        public async Task<IActionResult> PostFavorito(string pesquisa, [FromBody] List<long> id)
        {
            IList<Paciente> p = await BuscaFavoritoPost(id, pesquisa);

            if (p.Count == 0) return NoContent();
            return Ok(p);
        }


        [HttpGet]
        [Route("BuscaNomesPorId")]
        public async Task<IActionResult> BuscaNomesPorId(List<long> ids)
        {
            IList<Paciente> pacientes = await BuscaNomesPacientes(ids);

            if (pacientes.Count == 0) return NoContent();
            return Ok(pacientes);
        }

        private async Task<IList<Paciente>> BuscaMeusPacientes(int inicio, int quantidade, string pesquisa = "", bool mostraTodosPacientesPesquisa = false, bool filtrarPorPrestador = true)
        {
            IList<Paciente> _pacientes = new List<Paciente>();
            long idPrestador = User.Identity.GetPrestadorId();
            string idPrestadorFiltro = null;

            if (filtrarPorPrestador)
            {
                idPrestadorFiltro = BuscaPrestadorFiltro(false, mostraTodosPacientesPesquisa, idPrestador.ToString(), pesquisa, true);
            }

            using (var _db = new OracleConnection(_configuration["Banco"]))
            {
                await _db.OpenAsync();

                var strQuery = $"SELECT * FROM(SELECT ROWNUM rnum, a.* FROM (";
                strQuery += $@"SELECT DISTINCT
                                        IdPaciente,
                                        Nome,
                                        NomeSocial,
                                        DataNascimento,
                                        SexoS
                                    FROM {OwnerBanco}VHMED_LISTA_PACIENTES ";

                strQuery += idPrestadorFiltro;
                strQuery += BuscaFiltrosQuery(idPrestadorFiltro, pesquisa, isMeuPaciente: true);
                strQuery += " ORDER BY 2";

                strQuery += $" ) a WHERE ROWNUM <= {quantidade + inicio})  WHERE rnum > {inicio}";
                _pacientes = (await _db.QueryAsync<Paciente>(strQuery)).ToList();
            }

            return _pacientes;
        }

        private async Task<IList<Paciente>> BuscaPacientesInternados(int inicio, int quantidade, string pesquisa = "", bool mostraTodosPacientes = false, bool mostraTodosPacientesPesquisa = false)
        {
            IList<Paciente> _pacientes = new List<Paciente>();
            using (var _db = new OracleConnection(_configuration["Banco"]))
            {
                var idPrestador = User.Identity.GetPrestadorId();
                var strQuery = string.Empty;
                await _db.OpenAsync();
                string idPrestadorFiltro = BuscaPrestadorFiltro(mostraTodosPacientes, mostraTodosPacientesPesquisa, idPrestador.ToString(), pesquisa);
                strQuery = $@"SELECT IdPaciente,
                                        IdAtendimento,
                                        Nome,
                                        NomeSocial,
                                        Leito,
                                        SexoS,
                                        DataNascimento,
                                        Convenio,
                                        UnidadeAtendimento,
                                        Categoria,
                                        CorBox as CorBoxS,
                                        Tipo,
                                        Ordem
                                    FROM TABLE({OwnerBanco}fnc_hmed_lista_internados({idPrestadorFiltro}, 'N', {idPrestador}))";

                strQuery += BuscaFiltrosQuery(idPrestadorFiltro, pesquisa);

                _pacientes = (await _db.QueryAsync<Paciente>(strQuery)).AsList().Skip(inicio).Take(quantidade).ToList();
            }
            return _pacientes;
        }

        private async Task<IList<Paciente>> BuscaFavoritoPost(List<long> id, string pesquisa)
        {
            IList<Paciente> Pacientes = new List<Paciente>();

            string Ids = string.Join(", ", id.ToArray());
            string where = "";
            if (!string.IsNullOrEmpty(pesquisa))
                where = $@" AND UPPER(pesquisa) like UPPER('%{pesquisa.Trim().Replace(' ', '%')}%')";

            using (var db = new OracleConnection(_configuration["Banco"]))
            {
                await db.OpenAsync();
                var Query = $@"SELECT idpaciente,
                                             nome,
                                             nomesocial,
                                             datanascimento,
                                             sexos
                                             FROM {OwnerBanco}vhmed_lista_pacientes
                                             WHERE idpaciente IN({Ids}) {(pesquisa?.Length == 0 ? string.Empty : where)} ";

                Query += _configuration["FiltroEmpresa"].IsNotEmptyOrWhiteSpace() ? $" AND {_configuration["FiltroEmpresa"]}" : string.Empty;

                Pacientes = (await db.QueryAsync<Paciente>(Query)).ToList();
            }
            return Pacientes;
        }

        private string BuscaPrestadorFiltro(bool mostraTodosPacientes, bool mostraTodosPacientesPesquisa, string idPrestador, string pesquisa, bool isMeuPaciente = false)
        {
            if (mostraTodosPacientes || (mostraTodosPacientesPesquisa && pesquisa.IsNotEmptyOrWhiteSpace()))
                return (isMeuPaciente ? "" : "0");
            return (isMeuPaciente ? $"WHERE IdPrestador = {idPrestador}" : $"{idPrestador}");
        }

        private string BuscaFiltrosQuery(string idPrestadorFiltro, string pesquisa, bool isMeuPaciente = false)
        {
            string filtrosQuery = string.Empty;
            if (isMeuPaciente)
            {
                if (_configuration["FiltroEmpresa"].IsNotEmptyOrWhiteSpace())
                    filtrosQuery = $"{(idPrestadorFiltro.IsNotEmptyOrWhiteSpace() ? " AND" : " WHERE")} {_configuration["FiltroEmpresa"]}";

                if (pesquisa.IsNotEmptyOrWhiteSpace())
                    filtrosQuery += $"{(idPrestadorFiltro.IsNotEmptyOrWhiteSpace() || _configuration["FiltroEmpresa"].IsNotEmptyOrWhiteSpace() ? " AND" : " WHERE")} UPPER(Pesquisa) like UPPER('%{pesquisa.Trim().Replace(' ', '%')}%')";

                return filtrosQuery;
            }
            filtrosQuery += $"{(_configuration["FiltroEmpresa"].IsNotEmptyOrWhiteSpace() ? $"WHERE {_configuration["FiltroEmpresa"]}" : string.Empty)}";

            if (pesquisa.IsNotEmptyOrWhiteSpace())
            {
                if (_configuration["FiltroEmpresa"].IsNotEmptyOrWhiteSpace())
                    filtrosQuery += " AND";
                else
                    filtrosQuery += " WHERE";
                filtrosQuery += $" UPPER(Pesquisa) like UPPER('%{pesquisa.Trim().Replace(' ', '%')}%')";
            }
            filtrosQuery += $" ORDER BY {_configuration["OrdenacaoInternados"]}";
            return filtrosQuery;
        }

        private async Task<List<Paciente>> BuscaNomesPacientes(List<long> ids)
        {
            try
            {
                using (var db = new OracleConnection(_configuration["Banco"]))
                {
                    await db.OpenAsync();

                    var query = $@"select distinct idpaciente, nome, nomesocial from {OwnerBanco}VHMED_LISTA_PACIENTES where idpaciente in ({string.Join(", ", ids.ToArray())})";
                    return (await db.QueryAsync<Paciente>(query)).Distinct().ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}