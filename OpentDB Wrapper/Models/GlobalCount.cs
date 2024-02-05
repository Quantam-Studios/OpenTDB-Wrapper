using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTDB.Models
{
    public class GlobalCount
    {
        public int TotalQuestions { get; set; }
        public int TotalPendingQuestions { get; set; }
        public int TotalVerifiedQuestions { get; set; }
        public int TotalRejectedQuestions { get; set; }
        public List<GlobalCategoryCount> Categories { get; set; }
    }
}
