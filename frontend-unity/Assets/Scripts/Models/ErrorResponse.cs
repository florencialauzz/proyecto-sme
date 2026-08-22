using System;

namespace Sme.Models
{
    // Formato de error único de contratos/api-contract.md, sección 1.
    [Serializable]
    public class ErrorResponse
    {
        public string error;
        public string codigo;
    }
}
