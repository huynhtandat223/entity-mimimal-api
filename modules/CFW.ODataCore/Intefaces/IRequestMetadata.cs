using CFW.ODataCore.Models.Metadata;

namespace CFW.ODataCore.Intefaces;

public interface IRequestMetadata
{
    public MetadataAction Metadata { get; set; }
}
