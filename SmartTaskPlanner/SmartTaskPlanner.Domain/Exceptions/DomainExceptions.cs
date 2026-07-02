namespace SmartTaskPlanner.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

public class CircularDependencyException : DomainException
{
    public CircularDependencyException(string message) : base(message)
    {
    }
}

public class SelfDependencyException : DomainException
{
    public SelfDependencyException(string message) : base(message)
    {
    }
}

public class DependencyNotFoundException : DomainException
{
    public DependencyNotFoundException(string message) : base(message)
    {
    }
}

public class TaskNotFoundException : DomainException
{
    public TaskNotFoundException(string message) : base(message)
    {
    }
}
