using System;
using System.IO;
using Felix2D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FelixBuild
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    public static void Verify(){
        var s=new GameState();Check(s.Lives==3 && s.Level==1 && s.Remaining==12 && s.TimeLeft==60,"Initial state");
        for(int i=0;i<10;i++)s.Move(-1,-1);Check(s.Column==0 && s.Floor==0,"Lower grid bounds");
        for(int i=0;i<10;i++)s.Move(1,1);Check(s.Column==2 && s.Floor==3,"Upper grid bounds");
        Check(s.Repair() && s.Score==100,"Repair awards points");Check(!s.Repair() && s.Score==100,"No duplicate scoring");
        s.DamageWindow(2,3);Check(s.Broken[2,3],"Enemy damages a window");
        Check(s.Hit() && s.Lives==2,"Brick hit");Check(!s.Hit() && s.Lives==2,"Invulnerability");s.Tick(1.2f);Check(s.Hit(),"Protection expires");
        s.Restart();for(int f=0;f<4;f++){for(int c=0;c<3;c++){while(s.Column>c)s.Move(-1,0);while(s.Column<c)s.Move(1,0);s.Repair();}s.Move(0,1);}
        Check(s.Level==2 && s.Score==1200 && s.Remaining==12 && s.TimeLeft==80,"Level progression");
        s.Tick(100);Check(s.Over && s.TimeLeft==0,"Timeout");int points=s.Score;Check(!s.Repair() && s.Score==points,"No repairs after game over");
        s.Restart();s.Hit();s.Tick(1.2f);s.Hit();s.Tick(1.2f);s.Hit();Check(s.Over && s.Lives==0,"Lives game over");
        s.Restart();Check(!s.Over && s.Score==0 && s.Lives==3 && s.Level==1,"Restart clears run");
        bool invalid=false;try{s.Tick(-1);}catch(ArgumentOutOfRangeException){invalid=true;}Check(invalid,"Reject invalid elapsed time");
        Directory.CreateDirectory("../docs");File.WriteAllText("../docs/unity-verification.json","{\"engine\":\"Unity "+Application.unityVersion+"\",\"checksPassed\":"+checks+",\"result\":\"pass\"}");Debug.Log("Felix rule checks passed: "+checks);
    }
    [MenuItem("Felix/Prepare scene and verify")]
    public static void Prepare(){
        Verify();EditorSettings.defaultBehaviorMode=EditorBehaviorMode.Mode2D;
        PlayerSettings.companyName="Emad Obeidat";PlayerSettings.productName="Fix-It Felix Jr - Unity 2D";
        PlayerSettings.defaultScreenWidth=960;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Felix Game").AddComponent<FelixGame>();
        Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Arcade.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Arcade.unity",true)};AssetDatabase.SaveAssets();
    }
    [MenuItem("Felix/Build Windows")]
    public static void BuildWindows(){Prepare();Directory.CreateDirectory("../Builds/Windows");var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Arcade.unity"},"../Builds/Windows/Felix2D.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);}
}
