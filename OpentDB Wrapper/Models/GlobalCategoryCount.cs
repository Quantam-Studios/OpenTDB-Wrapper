using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTDB.Models
{
    public class GlobalCategoryCount
    {
        public int CategoryId { get; set; }
        public int TotalQuestions { get; set; }
        public int PendingQuestions { get; set; }
        public int VerifiedQuestions { get; set; }
        public int RejectedQuestions { get; set; }
    }
}
