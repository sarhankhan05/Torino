using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace VTool.addons.image.tool;

/**
    ToolPngMerge 工具功能详解

    主要功能
    图像合并：ToolPngMerge 是一个专门用于合并多个 PNG 图像的工具类，能够将场景中所有 Sprite2D 节点的纹理合并成一张大图，并将文件保存到
    (user://tool/png/merge)目录下
     
    核心特性
    1. 自动检测功能
    自动遍历当前节点的所有子节点
    识别并收集所有 Sprite2D 节点的纹理图像
    当没有找到任何 Sprite2D 节点时，会显示配置警告

    2. 灵活的合并选项
    合并方向：支持水平合并（默认）和垂直合并两种模式
    对齐方式：提供三种图像分布方式：居中对齐（0）、顶部对齐（1）、底部对齐（2）
    自适应尺寸：自动计算所有图像的最大宽高，统一调整到相同尺寸后再进行合并
 */

[Tool]
public partial class ToolPngMerge : Node2D
{
    /// <summary>
    /// 是否水平合并，默认为true（水平合并）
    /// </summary>
    [Export]
    public bool IsHorizontalMergeH = true;
    
    /// <summary>
    /// 合并后图像的最大宽度（自动计算子节点中的最大宽度）
    /// </summary>
    [Export]
    public int MaxWidth = 0;
    
    /// <summary>
    /// 合并后图像的最大高度（自动计算子节点中的最大高度）
    /// </summary>
    [Export]
    public int MaxHeight = 0;

    /// <summary>
    /// 图像分布方式：0-居中对齐，1-顶部对齐，2-底部对齐
    /// </summary>
    [Export]
    public int Distribution = 0;
    
    /// <summary>
    /// 执行合并操作的工具按钮，点击会调用Exec方法
    /// </summary>
    [ExportToolButton("EXEC", Icon = "Node2D")]
    public Callable ExecButton => Callable.From(Exec);
    
    public string ResultPath = "user://tool/png/merge";
    
    /// <summary>
    /// 执行图像合并操作的主要方法
    /// 遍历所有子节点，提取Sprite2D的纹理，统一尺寸后按指定方向合并
    /// </summary>
    public void Exec()
    {
        // 收集所有子节点中的图像数据
        Array<Image> imageFiles = new Array<Image>();
        Array<Node> children = GetChildren();

        if (children.Count <= 0)
        {
            GD.Print("没有找到任何Sprite2D节点");
            return;
        }
        
        for (int i = 0; i < children.Count; i++)
        {
            Node child = children[i];
            if (child is Sprite2D sprite2D)
            {
                GD.Print("合并图片: " +sprite2D.Name); // 输出精灵名称到控制台
                Texture2D texture2D = sprite2D.Texture;
                Image image = texture2D.GetImage();
                imageFiles.Add(image);
            }
        }
        
        // 计算所有图像的最大宽高作为标准尺寸
        foreach (Image img in imageFiles)
        {
            MaxWidth = Math.Max(MaxWidth, img.GetWidth());
            MaxHeight = Math.Max(MaxHeight, img.GetHeight());
        }
        
        // 创建调整大小后的图像数组
        Array<Image> newImageArray = new Array<Image>();

        // 将每个图像调整到统一尺寸，并根据分布设置定位
        foreach (Image img in imageFiles)
        {
            // 创建统一尺寸的新图像
            Image newImage = Image.CreateEmpty(MaxWidth, MaxHeight, false, Image.Format.Rgba8);
            
            // 计算居中对齐的偏移量
            int x = (MaxWidth - img.GetWidth()) / 2;
            int y = (MaxHeight - img.GetHeight()) / 2;
            
            // 根据分布参数调整垂直位置
            if (Distribution == 1)
            {
                // 顶部对齐
                y = 0;
            }
            else if (Distribution == 2)
            {
                // 底部对齐
                y = MaxHeight - img.GetHeight();
            }
            
            // 将原图复制到新图像的指定位置
            newImage.BlitRect(img, new Rect2I(0, 0, img.GetWidth(), img.GetHeight()), new Vector2I(x, y));
            newImageArray.Add(newImage);
        }

        // 构建输出路径
        string path = $"{ResultPath}/{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.png";
        
        // 确保目录存在，不存在则创建
        if (!DirAccess.DirExistsAbsolute(ResultPath))
        {
            GD.Print($"创建目录：{ResultPath}");
            Error dirAbsolute = DirAccess.MakeDirRecursiveAbsolute(ResultPath);
            if (dirAbsolute != Error.Ok)
            {
                // 获取错误描述文本
                GD.Print($"创建目录失败：{ResultPath}，错误代码：{dirAbsolute}");
                return;
            }
        }


        // 根据合并方向创建最终结果图像
        if (IsHorizontalMergeH)
        {
            // 水平合并：将所有图像从左到右拼接
            Image result = Image.CreateEmpty(MaxWidth * newImageArray.Count, MaxHeight, false, Image.Format.Rgba8);
            for (int i = 0; i < newImageArray.Count; i++)
            {
                Image img = newImageArray[i];
                // 将每张图像放置在正确的位置
                result.BlitRect(img, new Rect2I(0, 0, MaxWidth, MaxHeight), new Vector2I(MaxWidth * i, 0));
            }
            // 保存合并后的图像
            result.SavePng(path);
        }
        else
        {
            // 垂直合并：将所有图像从上到下拼接
            Image result = Image.CreateEmpty(MaxWidth, MaxHeight * newImageArray.Count, false, Image.Format.Rgba8);
            for (int i = 0; i < newImageArray.Count; i++)
            {
                Image img = newImageArray[i];
                // 将每张图像放置在正确的位置
                result.BlitRect(img, new Rect2I(0, 0, MaxWidth, MaxHeight), new Vector2I(0, MaxHeight * i));
            }

            // 保存合并后的图像
            result.SavePng(path);
        }
        
        // 输出最终图像的尺寸信息
        GD.Print($"Width:{MaxWidth} Height:{MaxHeight}");
    }
    
    
    public override string[] _GetConfigurationWarnings()
    {
        Array<string> warnings = new Array<string>();

        string[] configurationWarnings = base._GetConfigurationWarnings();
        if (configurationWarnings is not null && configurationWarnings.Length > 0)
        {
            foreach (string warning in configurationWarnings)
            {
                warnings.Add(warning);
            }
        }

        // 统计找到的Sprite2D数量
        Array<Node> children = GetChildren();
        int spriteCount = 0;
        for (int i = 0; i < children.Count; i++)
        {
            Node child = children[i];
            if (child is Sprite2D)
            {
                spriteCount++;
            }
        }
        
        // 如果没有找到任何Sprite2D节点，发出警告
        if (spriteCount == 0)
        {
            warnings.Add("没有找到任何Sprite2D节点，无法执行图像合并操作");
        }

        return warnings.ToArray();

    }

}
