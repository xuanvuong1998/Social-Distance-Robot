using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Database;
using System.Reactive.Linq;

namespace robot_head
{
    //In the current implementation firebase in winform only read message from database
    class FirebaseDB
    {
        //public static async Task Run()
        //{

        //    var client = new FirebaseClient("https://telepresence-np.firebaseio.com");
        //    var child = client.Child("annoucement");

        //    var observable = child.AsObservable<Annoucement>();

        //    // delete entire conversation list
        //    // await child.DeleteAsync();

        //    // subscribe to messages comming in, ignoring the ones that are from me
        //    var subscription = observable
        //        .Where(f => !string.IsNullOrEmpty(f.Key)) // you get empty Key when there are no data on the server for specified node
        //        .Subscribe(f => Console.WriteLine($"{f.Object.Message}"));

        //    while (true)
        //    {
        //       //doNothingHere just to keep task active
        //    }

        //    subscription.Dispose();
        //}

    }

}
