


using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using backend.Crawler;

public class Parser
{
    public Parser()
    {
        
    }

    public JsonTree parse_pipeline(string html)
    {
        var html_no_comments=this.RemoveComments(html);
        int html_start_pos=0;
        var len=html_no_comments.Length;
        // Console.WriteLine($"len is {len}");
        List<tag> elements=[];
        while (html_start_pos < len)
        {
            var tag= GetElement(html_no_comments, html_start_pos);

            if (tag.opening_tag_pos == -1)
            {
                break;
            }   

            elements.Add(tag);

            html_start_pos = tag.opening_tag_closing_pos + 1;
        }
        
         
        this.CleanTagName(elements);
        this.AddVoidClosingTags(elements);

        ////////////////////////////////////////////////////////////////////
        // this is for debugging liek chekign if we get an invalid html tags
        // this.GetUniqueElements(elements);
        ////////////////////////////////////////////////////////////////////

        Queue<tag> queue = new Queue<tag>(elements);
        //we dont need dectalrion tags 
        var declaratio_tags = queue.Dequeue();

        tag root_tag=queue.Dequeue();
        Node root=new(true,root_tag.name,root_tag.opening_tag_pos,root_tag.opening_tag_closing_pos,1,null);
        
        this.InitTree(root,queue);

        var json=this.JsonStruct(root,html_no_comments);

        return json;
    }

    string RemoveComments(string html)
    {
        StringBuilder result = new StringBuilder();

        int i = 0;

        while (i < html.Length)
        {
            // Check for <!--
            if (i + 3 < html.Length &&
                html[i] == '<' &&
                html[i + 1] == '!' &&
                html[i + 2] == '-' &&
                html[i + 3] == '-')
            {
                // Skip <!--
                i += 4;

                // Find -->
                while (i + 2 < html.Length &&
                    !(html[i] == '-' &&
                        html[i + 1] == '-' &&
                        html[i + 2] == '>'))
                {
                    i++;
                }

                // Skip -->
                if (i + 2 < html.Length)
                {
                    i += 3;
                }
            }
            else
            {
                result.Append(html[i]);
                i++;
            }
        }

        // Console.WriteLine($"html with no comments {result}");

        return result.ToString();
    }

    tag GetElement(string html,int start_pos)
    {
        int opening_tag_pos = -1;
        int opening_tag_closing_pos = -1;

        string tag_name = "";

        for (int i = start_pos; i < html.Length; i++)
        {
            // Find opening <
            if (opening_tag_pos == -1)
            {
                if (html[i] == '<')
                {
                    opening_tag_pos = i;
                    i++; // move after <

                    int tag_name_start = i;

                    // Find end of tag name
                    while (
                        i < html.Length &&
                        html[i] != ' ' &&
                        html[i] != '\t' &&
                        html[i] != '\n' &&
                        html[i] != '\r' &&
                        html[i] != '>'
                    )
                    {
                        i++;
                    }

                    tag_name = html.Substring(
                        tag_name_start,
                        i - tag_name_start
                    );
                }
            }

            // Find >
            if (opening_tag_pos != -1 && html[i] == '>')
            {
                opening_tag_closing_pos = i;
                break;
            }
        }

        // Console.WriteLine(opening_tag_pos);
        // Console.WriteLine(opening_tag_closing_pos);
        // Console.WriteLine(tag_name);
        var tag=new tag();
        tag.name=tag_name;;
        tag.opening_tag_pos=opening_tag_pos;
        tag.opening_tag_closing_pos=opening_tag_closing_pos;
        return tag;
    }
    
    void GetUniqueElements(List<tag> elements)
    {
        Dictionary<string, int> elementCount = new();

        foreach (var element in elements)
        {
            string tagName = element.name;

            if (elementCount.ContainsKey(tagName))
            {
                elementCount[tagName]++;
            }
            else
            {
                elementCount[tagName] = 1;
            }
        }

        foreach (var element in elementCount)
        {
            Console.WriteLine($"{element.Key}: {element.Value}");
        }
    }
   
    void CleanTagName(List<tag> elements)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            var tag = elements[i];

            if (tag.name.StartsWith("\\/"))
            {
                tag.name = tag.name.Substring(1);
            }

            elements[i] = tag;
        }
    }

    void AddVoidClosingTags(List<tag> elements)
    {
        HashSet<string> voidElements = new()
        {
            "area",
            "base",
            "br",
            "col",
            "embed",
            "hr",
            "img",
            "input",
            "link",
            "meta",
            "source",
            "track",
            "wbr"
        };

        for (int i = 0; i < elements.Count; i++)
        {
            var tag = elements[i];
            var closingTag = new tag();
            closingTag.name = "/" + tag.name;
            closingTag.opening_tag_closing_pos=tag.opening_tag_closing_pos;
            closingTag.opening_tag_pos=tag.opening_tag_pos;

            if (voidElements.Contains(tag.name))
            {
                elements.Insert(
                    i + 1,
                    closingTag
                );

                i++; // skip the newly inserted closing tag
            }
        }
    }

    void InitTree(Node node,Queue<tag> queue)
    {   
        Console.WriteLine($"{node.name}");

        if (queue.Count==0)
        {
            return;
        }

        var tag=queue.Dequeue();
        //if teh tab is clsing atg witht eh same naem tehn we end it adn this tag is teh sibling to node 
        if (node.name==tag.name.Substring(1))
        {
            
            var closing_tag_start_pos=tag.opening_tag_pos;
            var closing_tag_end_pos=tag.opening_tag_closing_pos;
            node.SetNodeClOsingPos(closing_tag_start_pos,closing_tag_end_pos);
            

            return;
        }
        //if it dosen tmahc then it s child node
        if (node.name != tag.name.Substring(1))
        {   
            var hieght=node.Hieght();
            var child_node=new Node(false,tag.name,tag.opening_tag_pos,tag.opening_tag_closing_pos,hieght,node);
            node.SetChild(child_node);
            InitTree(child_node,queue);
            // Child is finished, continue processing this node
            InitTree(node, queue);
        }

    }

    JsonTree JsonStruct(Node node,string html)
    {
        var json=new JsonTree();
        json.name=node.name;

        var content=this.GetContent(node,html);
        json.content=content;

        json.HashContent();

        json.children=[];
        foreach (var child in node.children)
        {
            json.children.Add(JsonStruct(child,html));
        }
        return json;
    }

    string GetContent(Node node,string html)
    {
        int start = node.opening_start_pos;

        int length =
            node.closing_end_pos -
            node.opening_start_pos +
            1;

        return html.Substring(start, length);
    }


}

struct tag{
    public string name;
    public int opening_tag_pos;
    public int opening_tag_closing_pos;
    
}



class Node
{
    bool is_root;
    public string name;
    int hieght;
    int children_count;
    public List<Node> children;
    public int opening_start_pos;
    public int opening_end_pos;
    public int closing_start_pos;
    public int closing_end_pos ;

    Node? parent;

    public Node(bool is_root,string name,int opening_start_pos,int opening_end_pos,int hieght,Node? parent)
    {
        this.children=[];
        this.children_count=0;
        this.opening_end_pos=opening_end_pos;
        this.opening_start_pos=opening_start_pos;
        this.hieght=hieght;
        this.is_root=is_root;
        this.name=name;
        this.parent=parent;
        
    }

    public Node GetParentNode()
    {
        return this.parent!;
    }


    public int Hieght()
    {
        return this.hieght;
    }

    public void SetNodeClOsingPos(int closing_start_pos,int closing_end_pos)
    {
        this.closing_start_pos=closing_start_pos;
        this.closing_end_pos=closing_end_pos;
    }

    public void SetChild(Node child)
    {
        this.children_count++;
        this.children.Add(child);
    }
}



public class JsonTree
{
    public string name;

    public string content;

    public string content_hash;

    public List<JsonTree> children;

    public JsonTree()
    {
        
    }

    public Dictionary<string, List<JsonTree>> CreateElementHashMap( Dictionary<string, List<JsonTree>> map)
    {
        if (!map.ContainsKey(this.name))
        {
            map[this.name] = new List<JsonTree>();
        }

        map[this.name].Add(this);

        foreach (var child in this.children)
        {
            child.CreateElementHashMap(map);
        }

        return map;
    }

    public void HashContent()
    {   
        string json=JsonSerializer.Serialize(this);
        byte[] bytes=SHA256.HashData(Encoding.UTF8.GetBytes(json));
        var hash= Convert.ToHexString(bytes);
        this.content_hash=hash;
    }

}




