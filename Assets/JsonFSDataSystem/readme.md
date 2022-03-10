# Json FS Data System

This is a powerful but simple way to save your game data. 
You can save a data as a file to a virtual disk container as simple as this:

```c#
public void Func()
{
    DataFile.CreateOrGet(new FSPath("/MyDatas/MyPosition.vec3"))
        .As<Vector3>(out var pos)
            transform.position = pos.Read();
        
    // ...
    
    pos.Write(transform.position);
}
```

Well, just like operating a disk file!