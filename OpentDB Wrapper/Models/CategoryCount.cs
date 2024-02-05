using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTDB.Models
{
    public class CategoryCount
    {
        public int CategoryId { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalEasyQuestions { get; set; }
        public int TotalMediumQuestions { get; set; }
        public int TotalHardQuestions { get; set; }
    }
}

