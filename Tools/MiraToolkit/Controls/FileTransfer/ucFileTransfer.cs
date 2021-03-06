﻿using System.Collections.Generic;
using System.Windows.Forms;
using MiraToolkit.Core;
using System.IO;
using static MiraToolkit.Core.MiraFileTransferExtensions;

namespace MiraToolkit.Controls.FileTransfer
{
    public partial class ucFileTransfer : UserControl
    {
        private const string c_LoadingTag = "_loading";

        private MiraDevice m_Device;

        private TreeNode m_RootNode;

        public ucFileTransfer(MiraDevice p_Device)
        {
            InitializeComponent();

            m_Device = p_Device;

            m_RootNode = new TreeNode
            {
                Text = "/",
                ToolTipText = "Root Node"
            };

            var s_LoadingNode = new TreeNode
            {
                Tag = c_LoadingTag,
                Text = "loading...",
            };

            m_RootNode.Nodes.Add(s_LoadingNode);

            tvDirectories.Nodes.Add(m_RootNode);
        }

        private void tvDirectories_BeforeExpand(object p_Sender, TreeViewCancelEventArgs p_Args)
        {
            var s_Node = p_Args.Node;
            if (s_Node.Nodes.Count != 1)
                return;

            var s_Tag = s_Node.Nodes[0].Tag as string;
            if (string.IsNullOrWhiteSpace(s_Tag))
                return;

            if (s_Tag != c_LoadingTag)
                return;

            // Clear the previous loading node
            s_Node.Nodes.Clear();

            // Get the full normalized path
            var s_Path = s_Node.FullPath.GetNormalizedPath();

            var s_Nodes = GetDirectoryNodes(s_Path);

            s_Node.Nodes.AddRange(s_Nodes);
        }

        private TreeNode[] GetDirectoryNodes(string p_Path)
        {
            var s_NodeList = new List<TreeNode>();

            var s_Entries = m_Device?.Connection?.GetDents(p_Path);
            if (s_Entries == null)
                return null;

            foreach (var l_Entry in s_Entries)
            {
                var l_EntryName = Path.GetFileName(l_Entry.Path);
                if (string.IsNullOrWhiteSpace(l_EntryName))
                    l_EntryName = l_Entry.Path;

                var l_NodeImageKey = GetFileImageKey((FileTransferDirentType)l_Entry.Type);
                var l_Node = new TreeNode
                {
                    ImageKey = l_NodeImageKey,
                    SelectedImageKey = l_NodeImageKey,
                    Text = l_EntryName,
                    Tag = l_Entry,
                    ToolTipText = l_Entry.Path
                };

                if ((FileTransferDirentType)l_Entry.Type == FileTransferDirentType.Directory)
                {
                    var l_LoadingNode = new TreeNode
                    {
                        Tag = c_LoadingTag,
                        Text = "loading...",
                    };

                    l_Node.Nodes.Add(l_LoadingNode);
                }

                s_NodeList.Add(l_Node);
            }

            return s_NodeList.ToArray();
        }

        private string GetFileImageKey(FileTransferDirentType p_Type)
        {
            switch (p_Type)
            {
                case FileTransferDirentType.Directory:
                    return "imgFolder";
                default:
                    return "imgFile";
            }
        }

        private void cmuDownload_Click(object p_Sender, System.EventArgs p_Args)
        {
            var s_Node = tvDirectories.SelectedNode;
            if (s_Node == null)
                return;


            var s_Dialog = new SaveFileDialog
            {
                FileName = s_Node.Text,
                Filter = "All Files (*.*)|*.*",
                Title = "Download File"
            };

            if (s_Dialog.ShowDialog() != DialogResult.OK)
                return;

            m_Device?.Connection?.ReadFile(s_Node.FullPath, s_Dialog.FileName);
        }

        private void cmuDelete_Click(object sender, System.EventArgs e)
        {
            var s_Node = tvDirectories.SelectedNode;
            if (s_Node == null)
                return;


            if (MessageBox.Show($"Are you sure you want to delete {s_Node.Text}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            m_Device?.Connection?.Delete(s_Node.FullPath);
        }

        private void cmuDecrypt_Click(object sender, System.EventArgs e)
        {
            var s_Node = tvDirectories.SelectedNode;
            if (s_Node == null)
                return;

            var s_Dialog = new SaveFileDialog
            {
                FileName = s_Node.Text,
                Filter = "All Files (*.*)|*.*",
                Title = "Save file as"
            };

            if (s_Dialog.ShowDialog() != DialogResult.OK)
                return;

            var s_Data = m_Device?.Connection?.DecryptExecutableFile(s_Node.FullPath);
            if (s_Data == null)
                return;

            File.WriteAllBytes(s_Dialog.FileName, s_Data);
        }

        private void cmuUpload_Click(object sender, System.EventArgs e)
        {
            // TODO: Implement
        }
    }
}
