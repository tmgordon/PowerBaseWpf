using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBaseWpf.Helpers
{
    [AttributeUsage(AttributeTargets.All)]
    public class MappingAttribute : Attribute
    {
        public List<string> AliasList = new List<string>();
        public MappingAttribute(string alias, string alias2, string alias3)
        {
            AliasList.Add(alias);
            AliasList.Add(alias2);
            AliasList.Add(alias3);
        }
        public MappingAttribute(string alias, string alias2)
        {
            AliasList.Add(alias);
            AliasList.Add(alias2);
        }
        public MappingAttribute(string alias)
        {
            AliasList.Add(alias);
        }

    }
}
