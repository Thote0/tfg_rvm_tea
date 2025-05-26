using Meta.WitAi;
using Meta.XR.BuildingBlocks;
#if UNITY_EDITOR // => Ignore from here to next endif if not in editor
using UnityEditor;
using Meta.XR.BuildingBlocks.Editor;
#endif
using Oculus.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.VisualScripting;

public class EventManager : MonoBehaviour
{
    bool productSelected = false;
    public TextAsset JsonData;
    public ProductList productList = new ProductList();
    public GameObject handMenuButtonPrefab;
    Vector3 scale = new Vector3(0.025f, 1, 0.025f);
    Vector3 startingRotInHandMenu = new Vector3(90, 0, -90);
    float spacing = (float)0.125;

    [System.Serializable]
    public class ProductInfo
    {
        public string name;
        public List<float> position;
        public List<float> rotation;
    }

    [System.Serializable]
    public class ProductList
    {
        public ProductInfo[] products;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Extrae la informacion del json creando un array de productos con su respectiva informacion
        productList = JsonUtility.FromJson<ProductList>(JsonData.text);

        // Obtener el producto y flecha y desactivar el render para que no se muestren
        GameObject prod = GameObject.FindGameObjectWithTag("product");
        showOrHideRender(prod, false);

        GameObject arrow = GameObject.FindGameObjectWithTag("arrow");
        showOrHideRender(arrow, false);

        // Inicializar posicion para los productos del menu y obtener el objeto con la tag menu
        Vector3 startingPosInHandMenu = new Vector3((float)0.1751, (float)0.0175, (float)0.125);
        GameObject menu = GameObject.FindGameObjectWithTag("menu");
        menu = menu.transform.GetChild(0).gameObject;
        int iteration = 0;

        // Bucle encargado de crear cada producto de la lista de productos obtenidos del json previamente
        foreach (var item in productList.products) {
            // Se crea una instancia del prefab del boton del menu y se actualiza el padre de este nuevo objeto al gameobject Hand Menu
            GameObject newProduct = Instantiate(handMenuButtonPrefab);
            newProduct.transform.SetParent(menu.transform);

            // Se actualiza el nombre del objeto al siguiente formato: "numeroDeIteracion-nombreDelProductoObtenidoDelJson" y luego se actualiza su rotacion y posicion
            newProduct.name = iteration + "-" + item.name;
            newProduct.transform.eulerAngles = startingRotInHandMenu;
            newProduct.transform.position = startingPosInHandMenu;

            // Cambiamos el material del MeshRender de la instancia creada al material del nombre del producto que estan creados previamente en la carpeta Resources/materiales botones
            List<Material> materials = new List<Material>();
            materials.Add(Resources.Load("materiales botones/material boton " + item.name, typeof(Material)) as Material);

            newProduct.GetComponentInChildren<MeshRenderer>().SetMaterials(materials);

            // Calculamos el resto de la siguiente iteracion del bucle y 3 para saber si se ha cambiado de fila
            // En caso de que el resto sea 0 reseteamos la posicion Z y actualizamos la X
            iteration++;

            Math.DivRem(iteration, 3, out int resto);
            if (resto == 0)
            {
                startingPosInHandMenu.x = startingPosInHandMenu.x - spacing;
                startingPosInHandMenu.z = spacing;
            }
            else
            {
                startingPosInHandMenu.z = startingPosInHandMenu.z - spacing;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Si hay un producto seleccionado actualiza donde apunta la flecha de la mano derecha
        if (productSelected) {
            updateArrowPointing();
        }

        // Se busca el objeto que tenga la tag selected y se actualiza la variable que indica si esta seleccionado
        GameObject selectedObj = GameObject.FindGameObjectWithTag("selected");
        if (selectedObj == null)
        {
            productSelected = true;
        }
        else {
            productSelected = false;
        }
    }

    public void completedProduct(GameObject prod) {

        // Desactiva el render del producto ya que al completarlo ya no tiene que aparecer
        showOrHideRender(prod, false);

        // Se elimina el objeto de la lista y se actualiza la lista de productos con sus posiciones
        GameObject selected = GameObject.FindGameObjectWithTag("selected");

        // Se obtiene tanto la posicion en el menu, como el nombre del producto
        string prodName = selected.name;
        string[] nameSplit = selected.name.Split("-");
        string positionInMenu = nameSplit[0];
        string productName = nameSplit[1];

        changeAllProductsPositionsInHandMenu(prodName);

        Destroy(selected);

        // Desactiva el render de la flecha
        GameObject arrow = GameObject.FindGameObjectWithTag("arrow");
        showOrHideRender(arrow, false);

        // Desactiva el render del marco
        GameObject marco = GameObject.FindGameObjectWithTag("marco");
        showOrHideRender(marco, false);

        productSelected = false;
    }

    public void updateProduct(GameObject obj) {
        // Busca el producto que haya seleccionado
        GameObject currentSelected = GameObject.FindGameObjectWithTag("selected");
        if (currentSelected != null) {
            currentSelected.tag = "Untagged";
        }

        // Busca el Event Manager en compilacion
        GameObject eventSys = GameObject.FindGameObjectWithTag("eventSystem");
        EventManager manager = eventSys.GetComponent<EventManager>();

        // Le pone la tag selected al producto de la lista seleccionado
        obj.tag = "selected";

        // encuentra el objeto con la tag product que es el plano creado en el lugar del producto seleccionado
        GameObject selected = GameObject.FindGameObjectWithTag("product");

        if (!productSelected) {
            showOrHideRender(selected, true);

            manager.productSelected = true;
        }

        // Antes de obtener la informacion del producto debemos saber el nombre del producto quitando el numero de posicion y separador "-" del nombre ya que los creamos en este formato
        string[] nameSplit = obj.name.Split("-");
        // Obtiene la informacion del producto seleccionado y crea el plano en el lugar del producto seleccionado del engine que hay en ejecucion/compilacion
        ProductInfo prod = manager.getProductInfoByName(nameSplit[1]);
        // Se utiliza el event manager en ejecucion/compilacion ya que si no utilizaria el del prefab y hace que las variables no esten inicializadaas
        manager.modifyProductWhenSelected(selected, obj, prod.name, listFloatToVec3(prod.position), listFloatToVec3(prod.rotation));

        // Copia la posicion del objeto seleccionado al marco y activamos el render
        GameObject marco = GameObject.FindGameObjectWithTag("marco");

        float posX = obj.transform.position.x;
        float posY = obj.transform.position.y;
        float posZ = obj.transform.position.z;

        Vector3 newPos = new Vector3(posX, (float)(posY - 0.0001), posZ);

        marco.transform.position = newPos;

        // Modifica la textura del producto seleccionable a la textura del objeto seleccionado
        selected.GetComponentInChildren<MeshRenderer>().material = obj.GetComponentInChildren<MeshRenderer>().material;

        showOrHideRender(marco, true);
    }

    // Modifica el GameObject con la etiqueta "product" con la posicion y rotacion pasada por parametro
    GameObject modifyProductWhenSelected(GameObject prod, GameObject obj, string name, Vector3 pos, Vector3 rot) {
        // MODIFICA EL PRODUCTO YA CREADO QUE ES UN INTERACTUABLE
        prod.name = name;
        prod.transform.position = pos;
        prod.transform.localEulerAngles = rot;

        // Muestra la flecha y hay producto seleccionado asi que ponemos la variable productSelected true para actualizar en el update la direccion de la flecha
        if (!productSelected)
            productSelected = true;

        GameObject arrow = GameObject.FindGameObjectWithTag("arrow");
        showOrHideRender(arrow, true);
        
        return prod;
    }

    // Actualiza donde apunta la flecha de la mano derecha
    void updateArrowPointing() {
        GameObject arrow = GameObject.FindGameObjectWithTag("arrow");
        GameObject product = GameObject.FindGameObjectWithTag("product");

        arrow.transform.LookAt(product.transform);
        arrow.transform.Rotate(90, 0, 0);
    }

    // Oculta o muestra la flecha dependiendo del valor pasado por parametro
    void showOrHideRender(GameObject obj, bool opt)
    {
        obj.GetComponentInChildren<MeshRenderer>().enabled = opt;
    }

    // Obtiene la informacion del producto dado un nombre de la lista de productos obtenida del json al iniciar la aplicacion
    ProductInfo getProductInfoByName(string name) {
        if(productList==null || productList.products==null || productList.products.Length==0)
            productList = JsonUtility.FromJson<ProductList>(JsonData.text);

        ProductInfo prod = null;

        for (int i=0; i<productList.products.Length && prod == null; i++) {
            if (name == productList.products[i].name) {
                prod = productList.products[i];
            }
        }

        return prod;
    }

    // Devuelve un Vector3 dada una Lista de tipo float (para cambiar la posicion de ProductInfo a Vector3 para poder usarla al crear un plano)
    Vector3 listFloatToVec3(List<float> l) {
        Vector3 v3 = new Vector3();

        for (int i = 0; i < l.Capacity; i++) {
            v3[i] = l[i];
        }

        return v3;
    }

    // Actualiza la posicion de todos los productos del menu al eliminar el producto cuando se seleccione tras obtenerlo en el supermercado
    int changeAllProductsPositionsInHandMenu(string prodName) {
        int pos = -1;

        // Get Hand Menu Buttons game object
        GameObject menu = GameObject.FindGameObjectWithTag("menu");
        menu = menu.transform.GetChild(0).gameObject;

        int productsInMenu = menu.transform.childCount;

        // Variables para los hijos del menu y nombre de los objetos
        string[] nameSplit;
        int childPosInName;
        Transform child;

        // Variables para las posiciones
        Vector3 lastChildLocalPos = new Vector3();
        Vector3 auxPosToCopy = new Vector3();

        // Recorre el bucle de los hijos del hand menu button para encontrar el objeto que queremos en el segun el nombre
        for (int i=0; i<productsInMenu; i++) {
            child = menu.transform.GetChild(i);

            // Se comprueba si el nombre del hijo contiene "-" para diferenciar si es un producto o el marco
            // - Si es un producto se comprobara si es el objeto que se busca o una posicion superior de este, para actualizar la posicion y nombre de los que se tengan que desplazar
            if (child.name.Contains("-")) {
                nameSplit = child.name.Split("-");
                childPosInName = int.Parse(nameSplit[0]);

                if (prodName == child.name) {
                    // Es el producto completado, guardamos la posicion en el menu y su posicion local para las siguientes iteraciones del bucle
                    pos = childPosInName;
                    lastChildLocalPos = child.localPosition;
                }
                else if (pos!=-1 && pos<childPosInName) {
                    // Ya se ha encontrado el producto completado y el hijo actual tiene mayor posicion que el completado, por tanto hay que actualizar el hijo
                    // Primero guardamos la posicion actual del hijo para poder copiarla luego en la variable lastChildLocalPos
                    auxPosToCopy = child.localPosition;

                    // Actualizamos el nombre y posicion del hijo
                    child.name = (childPosInName - 1) + "-" + nameSplit[1];
                    child.localPosition = lastChildLocalPos;

                    lastChildLocalPos = auxPosToCopy;
                }

            }
        }


        return pos;
    }

}