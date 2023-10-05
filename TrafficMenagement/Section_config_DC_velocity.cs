using System.Collections.Generic;
using System;
using System.Net.NetworkInformation;
using System.Collections.Specialized;
using System.Security;
using System.IO.Compression;

namespace Symulation
{
    // section config to opis w czasie
    // na wejsciach i wyjsciach 
    // dla wszystkich permutacji
    // plus dlugosc okresu danej permutacji
    public class Section_config_DC_velocity
    {
        private readonly CityDataStorage city;
        private CityDataStorage CityMapCopy;
        private readonly List<int> list_of_I_nodes;
        private readonly List<int> list_of_O_nodes;
        private List<Permutation> permutations;
        private readonly List<Permutation> priority_Perm;
        private readonly List<Permutation> start_Perm;
        private readonly List<Permutation> end_Perm;
        private List<Intersection> list_of_intersections;
        private List<Entrance> list_of_entrances;
        private List<Exit> list_of_exits;
        private List<int> list_of_tracks;
        private double period;

    public Section_config_DC_velocity(CityDataStorage city, List<int> list_of_I_nodes, List<int> list_of_O_nodes, List<Permutation> permutations,List<Permutation> priority_perm, List<Permutation> start_perm, List<Permutation> end_perm, List<int> list_of_tracks, double DC_length)
        {
            list_of_intersections = new List<Intersection>();
            list_of_entrances = new List<Entrance>();
            list_of_exits = new List<Exit>();
            
            this.list_of_tracks = list_of_tracks;
            // nie do konca rozumiem czemu zrobilem tu kopie
            this.city = city.DeepCopyCity();
            CityMapCopy = city.DeepCopyCity();

            this.list_of_I_nodes = list_of_I_nodes;
            this.list_of_O_nodes = list_of_O_nodes;
            this.permutations = permutations;
            
            priority_Perm = priority_perm;
            start_Perm = start_perm;
            end_Perm = end_perm;

            // a jakie tu chodzi komponenty 
            // exit entranci itp ? 
            define_components_of_section_schematics(permutations);
            define_traverses_length(permutations, DC_length);
            
            // teraz jest tak ze config sklada sie z tego co jest zapisane w componentach
            // skrzyzowan danej sekcji. ok moge to zmienic ewentualnie 


           

            // mam 2 metody

            // ok to jest zrobione zostalo zdebugowanie
            // moge tu dodac jeszcz to zeby odswiezyc copie miasta
            // bo ona moze byc pozmieniana przez inne metody
            // czas przejazdu permutacji przez dany noude
            // dodaje noude do mapy w odleglosci permutacji przed danym noudem
            // przygotowuje droge pomiedzy, i modyfikacje dla ostatniej kapsuly
            // robie profil pomiedzy i odczytuje czas przejazdu
            
            // czas przejazdu pomiedzy noudami
            // robie tak ze wyszukuje droge 
            // modyfikuje, mod dla pierwszego pojazdu 
            // generuje profil 
            // sprawdzam czas przejazdu 
        }

        /// <summary>
        /// Warning !! method is temporary for development only . work only for city with DC sections where throu permutation 
        /// is mumbered as 2, and is last permutation. 
        /// </summary>
        private void extend_ride_thru_section()
        {
                var entrance =  permutations[permutations.Count - 1].get_first_component_on_list();
                var exit = permutations[permutations.Count - 1].get_last_component_on_list();

                var cumulative_time_for_entrance = calculate_cumulative_time_for_config(entrance.get_config());
                var time_shift_of_thro_permutation = period - cumulative_time_for_entrance; 
                var speed_thru_entrance = permutations[permutations.Count-1].get_entrance_speed();
                var length_change_of_thru_permutation = time_shift_of_thro_permutation * speed_thru_entrance;

                // dla config entrance i exit dla ostatnich permutacji
                // zmieniam length += length shift
                // zmieniam czas konca += time shift 
                var entrance_config = entrance.get_config();
                entrance_config[entrance_config.Count-1][1] += length_change_of_thru_permutation;
                entrance_config[entrance_config.Count-1][3] += time_shift_of_thro_permutation;
                
                var exit_config = exit.get_config();
                exit_config[entrance_config.Count-1][1] += length_change_of_thru_permutation;
                exit_config[entrance_config.Count-1][3] += time_shift_of_thro_permutation;
                


        }

        public void shift_config(double shift)
        {
            for(int i=0; i < list_of_entrances.Count();i++)
            {
                list_of_entrances[i].shift_config_time(shift);
            }

            for(int i=0; i < list_of_exits.Count();i++)
            {
                list_of_exits[i].shift_config_time(shift);
            }
        }
        public double get_period()
        {
            return period;
        }

        public List<double[]> get_config_for_exit(int node_number)
        {
            var exit = get_exit_attached_to_node(node_number);

            return exit.get_config();
        }

        public List<double[]> get_config_for_entrance(int node_number)
        {
            var entrance = get_entrance_attached_to_node(node_number);
            return entrance.get_config();

        }

        // ta cala metoda jest do uproszczenia 
        // teraz jest to mega skomplikowane


        public void calculate_config(List<Permutation> permutations, bool print, int section_number)
        {
           foreach(Permutation permutation in permutations)
                calculate_timing_for_permutation(permutation);
           
            align_timing_of_all_permutations(permutations);
           
           foreach (Permutation permutation in permutations)
                define_config_for_permutation(permutation);
            // calculate period 
            calculate_config_period();
            // temp throu extension
            extend_ride_thru_section();
           
           // config jest zdefiniowany w entrance i exit danych sekcji 
           // config def
           // List<double[]>
           // [i][]  i -index permutacji ktora jedzie przez dany noud, kazda ma swoja macierz 
            // [i][0] - numer permutacji
            // [i][1] - dlugosc danej permutacji
            // [i][2] - czas startu 
            // [i][3] - czas konca

            // define config dla kazdego permutation 
        }

        /// <summary>
        /// Metoda liczy to jaki co jaki czas mazna powtorzyc wszystkie przejazdy danej sekcji 
        /// Oznacza to entrance lub exit o najwiekszej rozpietosci w czasie.
        /// </summary>
        private void calculate_config_period()
        {
            // ide po wszystkich entrance i exit i dla kazdego licze to czas calego prezjazdu 
            // wynik to najdluzszy czas
            double longest_time = 0;

            for(int i=0;i<list_of_entrances.Count;i++)
            {
                var temp_time = calculate_cumulative_time_for_config(list_of_entrances[i].get_config()); 
                if(temp_time> longest_time)
                    longest_time = temp_time;
            }
            
            for(int i=0;i<list_of_exits.Count;i++)
            {
                var temp_time = calculate_cumulative_time_for_config(list_of_exits[i].get_config()); 
                if(temp_time> longest_time)
                    longest_time = temp_time;
            }

            period = longest_time;
        }

        private double calculate_cumulative_time_for_config(List<double[]> config)
        {
            
            return config[config.Count-1][3] - config[0][2];
        }

        private void define_config_for_permutation(Permutation permutation)
        {            
            var number = permutation.get_number_of_permutation();
            var length = permutation.get_current_length_of_traverse();

            var entrance = permutation.get_first_component_on_list();
            var node = entrance.get_number_of_attached_noude();
            var(start, end) = permutation.read_travel_time_for_node(node);
            entrance.add_config_section(new double[]{number, length, start, end});
            
            var exit = permutation.get_last_component_on_list();
            node = exit.get_number_of_attached_noude();
            (start, end) = permutation.read_travel_time_for_node(node);
            exit.add_config_section(new double[]{number, length, start, end});
        }

        private void calculate_timing_for_permutation(Permutation permutation)
        {
                var city_first_v = city.DeepCopyCity();
                var route_first_v = permutation.Get_deep_copy_of_route();
                var city_last_v = city.DeepCopyCity();
                var route_last_v = permutation.Get_deep_copy_of_route();

                var mod_first_v = new DC_section_traverse_mod(city_first_v);
                var mod_last_v = new DC_section_traverse_mod(city_last_v);

                mod_first_v.add_mod_of_first_pod_in_a_traverse(permutation.get_current_length_of_traverse(), ref route_first_v);
                mod_last_v.add_mod_of_last_pod_in_a_traverse(permutation.get_current_length_of_traverse(), ref route_last_v);
            
                var profile_vehicle_first = new FastestProfile(route_first_v, city_first_v);
                var profile_vehicle_last = new FastestProfile(route_last_v, city_last_v);

                var (error_1, profile_first_v) = profile_vehicle_first.ProfileBetweenNodes();
                var (error_2, profile_last_v) = profile_vehicle_last.ProfileBetweenNodes();

                var time_shift = permutation.get_current_length_of_traverse()/permutation.get_entrance_speed();
                profile_vehicle_last.shift_profile_timing(ref profile_last_v,time_shift);

                permutation.set_travel_time_table_for_first_vehicle(profile_first_v);
                permutation.set_travel_time_table_for_last_vehicle(profile_last_v);       
        }

        
        private void align_timing_of_all_permutations(List<Permutation> list_of_permutations)
        {
            var list_of_fixed_perm = new List<Permutation>();
            list_of_fixed_perm.Add(list_of_permutations[0]);

            while(list_of_fixed_perm.Count!=list_of_permutations.Count)
                for(int i = 0; i < list_of_permutations.Count; i++)
                    for(int j = i + 1; j< list_of_permutations.Count;j++)
                        if(are_two_permutations_intersecting(list_of_permutations[i],list_of_permutations[j]))
                        {
                            var shift =  calc_allign_timing_of_permutation_pair(list_of_permutations[i],list_of_permutations[j]);
                            
                            if(list_of_fixed_perm.Contains(list_of_permutations[i]) && !list_of_fixed_perm.Contains(list_of_permutations[j]))
                            {
                                permutations[j].shift_travel_time(shift+0.0002);
                                list_of_fixed_perm.Add(permutations[j]);
                            }
                            if(!list_of_fixed_perm.Contains(list_of_permutations[i]) && list_of_fixed_perm.Contains(list_of_permutations[j]))
                            {
                                permutations[i].shift_travel_time(-shift-0.0002);
                                list_of_fixed_perm.Add(permutations[i]);

                            }
                            // inna sytuacja oznacza ze nastapil kontakt pomiedzy permutacjami bez fix czyli 
                            // bez relacji z pierwsza permutacja lub obie permutacje sa juz fixed. 
                            // Wiec nieprzesowam wtedy nic 
                        }
        }

        /// <summary>
        /// metoda szuka czy istnieje przynajmniej jeden wspolny track dla danych permutacji
        /// co z opca gdzie dedzie tak ze przecinaja sie dwa traki poprastu bez mozliwosci skrecania
        /// to bedzie wymagac dodatkowego oznaczenie w noudach. narazie tego nierobie. wiec narazie metoda 
        /// by zawiodla
        /// </summary>
        /// <param name="first_per"></param>
        /// <param name="second_per"></param>
        /// <returns></returns>
        private bool are_two_permutations_intersecting(Permutation first_per, Permutation second_per)
        {
            
            var route_one = first_per.Get_route();
            var route_two = second_per.Get_route();
            
            for(int i=0;i<route_one.Count();i++)
            {
                for(int j=0; j<route_two.Count();j++)
                {
                    if (route_one[i][2] == route_two[j][2])
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Metoda ustawia permutacje za soba w kolejnosci first_per jako pierwsza
        /// dadatkowo pomiedzy permutacjami jest dodawany czas = 0.00002[s] tak zeby cyfrowa miec pewnasc ze obie permutacje nie nachodza na siebie
        /// </summary>
        /// <param name="first_per"></param>
        /// <param name="second_per"></param>
        private double calc_allign_timing_of_permutation_pair(Permutation first_per, Permutation second_per)
        {
            List<int> list_of_common_nodes = generate_list_of_common_nodes_of_routes(first_per.Get_route(), second_per.Get_route());

            double needed_shift = -200000; // arb low value 

            for(int i = 0; i < list_of_common_nodes.Count(); i++)
            {
                var node_shift = calculate_shift_for_node(first_per,second_per,list_of_common_nodes[i]);
                if(node_shift > needed_shift)
                    needed_shift = node_shift;
            }

            return needed_shift;
        }

        private double calculate_shift_for_node(Permutation first_per, Permutation second_per, int node)
        {
            var (first_start, first_end) = first_per.read_travel_time_for_node(node);
            var (second_start, second_end) = second_per.read_travel_time_for_node(node);

            return first_end - second_start;
        }
        
        private List<int> generate_list_of_common_nodes_of_routes(int[][]route_one, int[][] route_two)
        {
            HashSet<int> nodes_of_route_one = generate_set_of_route_nodes(route_one);
            HashSet<int> nodes_of_route_two = generate_set_of_route_nodes(route_two);

            nodes_of_route_one.IntersectWith(nodes_of_route_two);
            
            return nodes_of_route_one.ToList();
        }

        private HashSet<int> generate_set_of_route_nodes(int [][] route)
        {
            var set = new HashSet<int>();

            for(int i = 0; i < route.Count(); i++)
            {
                set.Add(route[i][0]);
                set.Add(route[i][1]);
            }

            return set;
        }

       
        public void define_components_of_section_schematics(List<Permutation> section_permutations)
        {
            //go along route of all permutations and add entrances, intersection and exits


            var route_nodes = new List<int>();
            foreach(Permutation n in section_permutations)
            {
                route_nodes = get_list_of_nodes_along_route(n.Get_route());

                for (int i = 0; i < route_nodes.Count; i++)
                {
                    check_node(route_nodes[i],n);
                }

            }
        }

        private List<int> get_list_of_nodes_along_route(int[][] route)
        {
            var new_list =new List<int>();

            for(int i = 0 ; i < route.GetLength(0) ; i++)
            {
                new_list.Add(route[i][0]);
            }

            new_list.Add(route[route.GetLength(0)-1][1]);

            return new_list;
        }
        private void check_node(int node_number, Permutation permutation)
        {
            bool is_this_a_start_node = permutation.is_node_a_start_node(node_number);
            bool is_this_an_end_node = permutation.is_node_an_end_node(node_number);
            // tu jest problem bo jak dodje schematic component do permutacji
            // to dla stacji dodaje jednoczasnie exit i start
            // a w przypadku stacji powinenem dodawac tylko start jesli dana permutacja sie tam zaczyna
            // i tylko koniec jesli dana permutacja sie tam konczy


            if (is_node_an_entrance(node_number) && is_this_a_start_node)
            {
                Entrance entrance;
                if (!is_there_already_antrance_attached_to_node(node_number))
                {
                    
                    list_of_entrances.Add(new Entrance(node_number));
                }
                add_per_to_entrance(node_number, permutation);
            }


            var (is_node_intersection, type) = is_node_an_intersection(node_number);//type 1 convergence, 2 Divergence
            if (is_node_intersection)
            {
                Intersection intersection;
                if (!is_there_intersection_allready_attached_to_node(node_number))
                {
                    list_of_intersections.Add(new Intersection(node_number, type));
                }

                add_per_to_intersection(node_number, permutation);
                //sort per required patern
                //for now it will be sort and reverse
                //intersection = get_intersection_attached_to_node(node_number);
                //intersection.sort_permutations_by_number();
            }

            if (is_node_an_exit(node_number) && is_this_an_end_node)
            {
                Exit exit;
                if (!is_there_exit_allready_attached_to_node(node_number))
                {
                    list_of_exits.Add(new Exit(node_number));
                }

                add_per_to_exit(node_number, permutation);
                //sort per required patern
                //for now it will be sort and reverse
                //exit = get_exit_attached_to_node(node_number);
                //exit.sort_permutations_by_number();
            }

        }
        public void define_traverses_length(List<Permutation> permutations, double length_trav_DC)
        {

            double max_temp_length = length_trav_DC;
            {
                foreach(Permutation m in permutations)
                {
                    //max_temp_length = m.get_max_length_of_traverse();

                    //m.set_current_length_of_traverse(max_temp_length - 1);



                    //switch (m.get_number_of_permutation())
                    //{
                    //    case 0:
                    //        max_temp_length = 17;
                    //        break;
                    //    case 1:
                    //        max_temp_length = 17;
                    //        break;
                    //    case 2:
                    //        max_temp_length = 17;
                    //        break;
                    //    case 3:
                    //        max_temp_length = 80; //sort
                    //        break;
                    //    case 4:
                    //        max_temp_length = 17;
                    //        break;
                    //    case 5:
                    //        max_temp_length = 15;
                    //        break;
                    //    case 6:
                    //        max_temp_length = 15;
                    //        break;
                    //    case 7:
                    //        max_temp_length = 80; //sort
                    //        break;
                    //    case 8:
                    //        max_temp_length = 18;
                    //        break;
                    //    case 9:
                    //        max_temp_length = 13;
                    //        break;
                    //    case 10:
                    //        max_temp_length = 12;
                    //        break;
                    //    case 11:
                    //        max_temp_length = 80;//sort
                    //        break;
                    //    case 12:
                    //        max_temp_length = 15;
                    //        break;
                    //    case 13:
                    //        max_temp_length = 14;
                    //        break;
                    //    case 14:
                    //        max_temp_length = 11;
                    //        break;
                    //    case 15:
                    //        max_temp_length = 80; //sort
                    //        break;

                    //}

                    m.set_current_length_of_traverse(max_temp_length);

                }
            }
        }
        
        private Entrance get_entrance_attached_to_node(int node)
        {
            foreach(Entrance n in list_of_entrances)
            {
                if (n.get_number_of_attached_noude() == node)
                {
                    return n;
                }
            }
            return null;
        }
        private Exit get_exit_attached_to_node(int node)
        {
            foreach(Exit n in list_of_exits)
            {
                if(n.get_attached_node_number() == node)
                {
                    return n;
                }
            }
            return null;
        }

        private bool is_node_an_entrance(int node_number)
        {
            if (list_of_I_nodes.Contains(node_number))
                return true;
            else
                return false;
        }
        private bool is_there_already_antrance_attached_to_node(int node_number)
        {
            int node;
            foreach (Entrance n in list_of_entrances)
            {
                node = n.get_number_of_attached_noude();
                if (node == node_number)
                    return true;
            }
            return false;
        }

        private bool is_node_an_exit(int node_number)
        {
            if (list_of_O_nodes.Contains(node_number))
                return true;
            else
                return false;
        }
        private bool is_there_exit_allready_attached_to_node(int node_number)
        {
            foreach(Exit n in list_of_exits)
            {
                if (n.get_attached_node_number() == node_number)
                    return true;
            }
            return false;
        }
        
        private void add_per_to_entrance(int node_number, Permutation permutation)
        {
            foreach (Entrance n in list_of_entrances)
            {
                if (n.get_number_of_attached_noude() == node_number)
                {
                    n.add_permutation_to_list(permutation);
                    permutation.add_component_to_list(n);
                }
            }
        }
        private void add_per_to_exit(int node_number, Permutation permutation)
        {
            foreach(Exit n in list_of_exits)
            {
                if (n.get_attached_node_number() == node_number)
                {
                    n.add_permutation_to_list(permutation);
                    permutation.add_component_to_list(n);
                }
            }
        }
        private void add_per_to_intersection(int node_number, Permutation permutation)
        {
            foreach(Intersection n in list_of_intersections)
            {
                if (n.get_number_of_attached_noude() == node_number)
                {
                    n.add_permutation_to_list(permutation);
                    permutation.add_component_to_list(n);
                }
            }
        }
        private (bool, int) is_node_an_intersection(int node_number)
        {
            return city.is_node_an_intersection_plus_type(node_number);
        }
        private bool is_there_intersection_allready_attached_to_node(int node_number)
        {
            foreach(Intersection n in list_of_intersections)
            {
                if (n.get_number_of_attached_noude() == node_number)
                    return true;
            }
            return false;
        }
    }

}
