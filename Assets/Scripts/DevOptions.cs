using System.Collections;
using System.Collections.Generic;
using UnityEngine;





public class DevOptions : MonoBehaviour
{
    public class OptionsHolder<T> where T : DevOptionsObj {
        T _o;
        public T o {
            get {
                if (_o == null) _o = Get<T>();
                return _o;
            }
        }
    }


    class OptionsDictionary {
        public Dictionary<System.Type, DevOptionsObj> options = new Dictionary<System.Type, DevOptionsObj>();

        public T Get<T> () where T : DevOptionsObj {
            return (T)options[typeof(T)];
        }

        public void Rebuild (DevOptionsObj[] new_objs) {
            options.Clear();
            for (int i = 0; i < new_objs.Length; i++) {

            
                options.Add(new_objs[i].ParentType(), new_objs[i]);
            }
        } 


        /**
        
        
typeof(Typ).BaseType.Name
TypeOf(Typ).GetType().BaseType.Name
this.GetType().DeclaringType.Name
obj.GetType().BaseType.Name
         */


    };

    static DevOptions _instance;
    static DevOptions instance {
        get {
            if (_instance == null) {
                _instance = GameObject.FindObjectOfType<DevOptions>();
            }
            return _instance;
        }
    }


    static OptionsDictionary options_dict = new OptionsDictionary();

    public static T Get<T> () where T : DevOptionsObj {
        if (options_dict.options.Count == 0) {
            instance.Rebuild ();
        }
        return options_dict.Get<T>();
    }


    void Rebuild () {
        options_dict.Rebuild(all_options);
    }

    public DevOptionsObj[] all_options;

    private void Awake()
    {
        Rebuild();
    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
