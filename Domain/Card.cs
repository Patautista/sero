using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public record Card
    {
        public Sentence NativeSentence { get; set; }
        public Sentence TargetSentence { get; set; }

        [ForeignKey("Tags")]
        public ICollection<Tag> Tags { get; set; }
    }
}
