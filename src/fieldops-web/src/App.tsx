import { useState, useEffect } from "react";
import Dashboard from "./Dashboard";
import LoginPage from "./LoginPage";
import { getCurrentUser,logout, type AuthenticatedUser } from "./auth";

export default function App(){
    const [user,setUser] = useState<AuthenticatedUser | null>(() => getCurrentUser(),);

    useEffect(()=> {
        function handleUnauthorized(){
            setUser(null);
        }
        window.addEventListener("fieldops:unauthorized",handleUnauthorized);

        return ()=> {
            window.removeEventListener("fieldops:unauthorized",handleUnauthorized);
        };
    },[]);

    function handleLogout(){
        logout();
        setUser(null);
    }

    if(!user){
        return <LoginPage onLogin={setUser} />;
    }

    return(
        <>
            <div className="session-bar">
                <span>
                    Signed in as {" "}
                    <strong>{user.firstName} {user.lastName}</strong>
                </span>
                <button type="button" onClick={handleLogout}>
                    Sign out
                </button>
            </div>

            <Dashboard />
        </>
    );
}
