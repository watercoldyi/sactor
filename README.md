# sactor
一个简单易用的Actor框架,基于C#   
#编译
使用VirsualStudio 2013
#使用
SActor.Threads 设置调度器线程数   
SActor.Logger 设置日志文件路径，当为空时使用控制台输出日志    
SActor.Init() 初始化库    
SActor.Launch() 启动一个actor   
SActor.Kill() 杀死一个actor   
#actor编写    
继承SActActor类，直接定义相应接口即可.    
SActActor.Send() 发送一条消息给指定actor   
SActActor.Call() 发送一条需要返回值的消息给指定actor
