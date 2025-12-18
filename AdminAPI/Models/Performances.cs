using Postgrest.Attributes;
using Postgrest.Models;

namespace JFBostonAdminAPI.Models;

// Name of supabase table holding stage events
[Table("stage_events")]
public class Performances : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("performance_name")]
    public required string Name { get; set; }

    [Column("start_time")]
    public required DateTime StartTime { get; set; }
}