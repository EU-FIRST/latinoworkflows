/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Tree.cs
 *  Desc:    Auxiliary classes for Document/DocumentCorpus HTML output
 *  Created: Dec-2010
 *
 *  Author:  Jasmina Smailovic
 *
 ***************************************************************************/

using System.Collections.Generic;

namespace Latino.Workflows.TextMining
{
    /* .-----------------------------------------------------------------------
       |
       |  Class Tree<T>
       |
       '-----------------------------------------------------------------------
    */
    public class Tree<T> : TreeNode<T>
    {
        public Tree()
        {           
        }

        public Tree(T rootValue)
        {
            Value = rootValue;
        }
    }

    /* .-----------------------------------------------------------------------
       |
       |  Class TreeNodeList<T>
       |
       '-----------------------------------------------------------------------
    */
    public class TreeNodeList<T> : List<TreeNode<T>>
    {
        private TreeNode<T> mParent;

        public TreeNodeList(TreeNode<T> parent)
        {
            this.mParent = parent;
        }

        public new TreeNode<T> Add(TreeNode<T> node)
        {
            base.Add(node);
            node.Parent = mParent;
            return node;
        }

        public TreeNode<T> Add(T value)
        {
            return Add(new TreeNode<T>(value));
        }
        
    }

    /* .-----------------------------------------------------------------------
       |
       |  Class TreeNode<T>
       |
       '-----------------------------------------------------------------------
    */
    public class TreeNode<T>
    {

        public TreeNode()
        {            
            Parent = null;
            Children = new TreeNodeList<T>(this);
        }

        public TreeNode(T value)
        {
            this.Value = value;
            Parent = null;
            Children = new TreeNodeList<T>(this);
        }

        public TreeNode(T value, TreeNode<T> parent)
        {
            this.Value = value;
            this.Parent = parent;
            Children = new TreeNodeList<T>(this);          
        }

        private TreeNode<T> mParent;
        public TreeNode<T> Parent
        {
            get { return mParent; }
            set
            {
                if (value == mParent)
                {
                    return;
                }

                if (mParent != null)
                {
                    mParent.Children.Remove(this);
                }

                if (value != null && !value.Children.Contains(this))
                {
                    value.Children.Add(this);
                }

                mParent = value;
            }
        }

        public bool HasChild(T child)
        {            
            for (int i = 0; i < this.Children.Count; i++)
            {
                if (this.Children[i].Value.Equals(child))
                    return true;
            }

            return false;
        }

        public TreeNode<T> GetChild(T child)
        {
            for (int i = 0; i < this.Children.Count; i++)
            {
                if (this.Children[i].Value.Equals(child))
                    return this.Children[i];
            }

            return null;
        }

        public int CountTreeLeaves()
        {
            return CountLeavesInSubTree(this);            
        }

        private int CountLeavesInSubTree(TreeNode<T> node)
        {
            if (node.mChildren.Count == 0) { return 1; }
            int count = 0;
            foreach (TreeNode<T> child in node.mChildren)
            {
                count += CountLeavesInSubTree(child);
            }
            return count;
        }




        public TreeNode<T> Root
        {
            get
            {  
                TreeNode<T> node = this;
                while (node.Parent != null)
                {
                    node = node.Parent;
                }
                return node;
            }
        }

        private TreeNodeList<T> mChildren;
        public TreeNodeList<T> Children
        {
            get { return mChildren; }
            private set { mChildren = value; }
        }

        private T mValue;
        public T Value
        {
            get { return mValue; }
            set { mValue = value;}
        }

        private T mElements;
        public T Elements
        {
            get { return mElements; }
            set { mElements = value; }
        }

        private T mFeatures;
        public T Features
        {
            get { return mFeatures; }
            set { mFeatures = value; }
        }

        public int Depth
        {
            get
            {
                int depth = 0;
                TreeNode<T> node = this;
                while (node.Parent != null)
                {
                    node = node.Parent;
                    depth++;
                }
                return depth;
            }
        }


        
    }
}
