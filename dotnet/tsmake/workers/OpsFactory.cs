using tsmake.data_models;

namespace tsmake.workers;

public interface IOpsFactory
{
    IFileSystem CurrentFileSystem { get; }
    
    INormalizer NewNormalizer();
    ITokenTransformer NewTokenTransformer();
    IDirectiveProcessor NewDirectiveProcessor();
    IDocumentationExtractor NewDocumentationExtractor();
}

public class OpsFactory(IFileSystem currentFileSystem, IAssemblerOptions assemblerOptions) : IOpsFactory
{
    private IAssemblerOptions _options = assemblerOptions;

    public IFileSystem CurrentFileSystem { get; } = currentFileSystem;
    
    public INormalizer NewNormalizer()
    {
        return new Normalizer(this._options.LineEndingsType);
    }

    public ITokenTransformer NewTokenTransformer()
    {
        return new TokenTransformer(this._options.TokenExclusionDirectives);
    }

    public IDirectiveProcessor NewDirectiveProcessor()
    {
        return new DirectiveProcessor();
    }

    public IDocumentationExtractor NewDocumentationExtractor()
    {
        throw new NotImplementedException("DocumentationExtractor is not implemented in C# yet.");
    }
}