interface IGreeter
{
  greet (name : string) : string
}

class DefaultGreeter implements IGreeter
{
  get name() : string 
  {
    return 'A name';
  }
  
  greet(name : string)
  {
    return 'Hello ' + name;
  }
}