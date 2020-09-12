using System.Linq;
using System.Linq.Expressions;
using Cypretex.Data.Filters.Parsers.Linq;

namespace Cypretex.Data.Filters
{
    public static class WhereComparator
    {
        public const string NONE = "NONE";
        public const string EQUALS = "EQUALS";
        public const string EQ = "EQ";
        public const string NOT_EQUALS = "NOT_EQUALS";
        public const string NEQ = "NEQ";
        public const string IN = "IN";
        public const string NOT_IN = "NOT_IN";
        public const string NIN = "NIN";
        public const string BETWEEN = "BETWEEN";
        public const string BTW = "BTW";
        public const string NOT_BETWEEN = "NOT_BETWEEN";
        public const string NBTW = "NTW";
        public const string IS_NULL = "IS_NULL";
        public const string N = "N";
        public const string NOT_NULL = "NOT_NULL";
        public const string NN = "NN";
        public const string LESS_THAN = "LESS_THAN";
        public const string LT = "LT";
        public const string LESS_OR_EQUALS = "LESS_OR_EQUALS";
        public const string LE = "LE";
        public const string GREATHER_THAN = "GHREATHER_THAN";
        public const string GT = "GT";
        public const string GREATHER_OR_EQUALS = "GREATHER_OR_EQUALS";
        public const string GE = "GE";

        // STRING only
        public const string STARTS_WITH = "STARTS_WITH";
        public const string SW = "SW";

        public const string NOT_STARTS_WITH = "NOT_STARTS_WITH";
        public const string NSW = "NSW";

        public const string ENDS_WITH = "ENDS_WITH";
        public const string EW = "EW";

        public const string NOT_ENDS_WITH = "NOT_ENDS_WITH";
        public const string NEW = "NEW";

        public const string CONTAINS = "CONTAINS";
        public const string CN = "CN";
        public const string NOT_CONTAINS = "NOT_CONTAINS";
        public const string NC = "NC";
        public const string EMPTY = "EMPTY";
        public const string EMP = "EMP";
        public const string NOT_EMPTY = "NOT_EMPTY";
        public const string NEMP = "NEMP";
        public const string NULL_OR_EMPTY = "NULL_OR_EMPTY";
        public const string NLEMP = "NLEMP";
        public const string NOT_NULL_OR_EMPTY = "NOT_NULL_OR_EMPTY";
        public const string NNLEMP = "NNLEMP";
        public const string REGEX = "REGEX";
        public const string RE = "RE";
        public const string NOT_REGEX = "NOT_REGEX";
        public const string NRE = "NRE";
    }
}