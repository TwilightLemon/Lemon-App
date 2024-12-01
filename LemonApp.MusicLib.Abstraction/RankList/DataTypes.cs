using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Abstraction.RankList;

public static class DataTypes
{
    public class RankListInfo
    {
        public string Name { set; get; }
        public string CoverUrl { set; get; }
        public string Id { set; get; }
        public string Description { set; get; }
        public List<string> Content { set; get; }
    }
}
